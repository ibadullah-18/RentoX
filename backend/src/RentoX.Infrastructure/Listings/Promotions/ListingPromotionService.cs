using System.Data;
using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Listings.Promotions;
using RentoX.Application.Wallets;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Listings.Promotions;
using RentoX.Domain.Listings.Promotions.Enums;
using RentoX.Domain.Wallets;
using RentoX.Domain.Wallets.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Listings.Promotions;

public sealed class ListingPromotionService(
    RentoXDbContext dbContext,
    IClock clock,
    IListingPromotionPricingService pricingService,
    IWalletService walletService)
    : IListingPromotionService
{
    public async Task<PurchaseListingPromotionResult>
        PurchaseAsync(
            PurchaseListingPromotionCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.OwnerId == Guid.Empty ||
            command.ListingId == Guid.Empty)
        {
            throw new DomainException(
                "Owner and listing ids are required.");
        }

        if (!Enum.IsDefined(
                typeof(ListingPromotionType),
                command.Type))
        {
            throw new DomainException(
                "Listing promotion type is invalid.");
        }

        if (string.IsNullOrWhiteSpace(
                command.IdempotencyKey))
        {
            throw new DomainException(
                "Idempotency key is required.");
        }

        string idempotencyKey =
            command.IdempotencyKey.Trim();

        if (idempotencyKey.Length >
            WalletTransaction.MaximumIdempotencyKeyLength)
        {
            throw new DomainException(
                "Idempotency key is too long.");
        }

        ListingPromotionType type =
            (ListingPromotionType)command.Type;

        ListingPromotionPricingResult pricing =
            pricingService.GetPricing(type);

        await using var databaseTransaction =
            await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        Listing listing =
            await dbContext.Listings
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == command.ListingId &&
                        item.OwnerId == command.OwnerId,
                    cancellationToken)
            ?? throw new DomainException(
                "Listing was not found.");

        WalletTransaction? existingTransaction =
            await dbContext.WalletTransactions
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.IdempotencyKey ==
                        idempotencyKey,
                    cancellationToken);

        if (existingTransaction is not null)
        {
            PurchaseListingPromotionResult
                existingResult =
                    await GetExistingResultAsync(
                        listing,
                        type,
                        existingTransaction,
                        cancellationToken);

            await databaseTransaction.CommitAsync(
                cancellationToken);

            return existingResult;
        }

        DateTimeOffset now = clock.UtcNow;

        if (listing.Status != ListingStatus.Active ||
            !listing.ExpiresAtUtc.HasValue ||
            listing.ExpiresAtUtc.Value <= now)
        {
            throw new DomainException(
                "Only an unexpired active listing can be promoted.");
        }

        DateTimeOffset? endsAtUtc = null;

        if (type == ListingPromotionType.Vip)
        {
            int durationDays =
                pricing.DurationDays
                ?? throw new DomainException(
                    "VIP duration is not configured.");

            endsAtUtc =
                now.AddDays(durationDays);

            if (listing.ExpiresAtUtc.Value <
                endsAtUtc.Value)
            {
                throw new DomainException(
                    "The listing must remain active for the full VIP period.");
            }

            bool alreadyVip =
                await dbContext.ListingPromotions
                    .AsNoTracking()
                    .AnyAsync(
                        item =>
                            item.ListingId == listing.Id &&
                            item.Type ==
                            ListingPromotionType.Vip &&
                            item.StartsAtUtc <= now &&
                            item.EndsAtUtc > now,
                        cancellationToken);

            if (alreadyVip)
            {
                throw new DomainException(
                    "An active VIP promotion already exists for this listing.");
            }
        }

        WalletOperationResult debit =
            await walletService.DebitAsync(
                new DebitWalletCommand(
                    command.OwnerId,
                    pricing.Price,
                    WalletTransactionType.PromotionFee,
                    "Listing promotion fee",
                    listing.Id,
                    idempotencyKey),
                cancellationToken);

        if (debit.WasAlreadyProcessed)
        {
            throw new DomainException(
                "This promotion payment was already processed. Retry the request.");
        }

        ListingPromotion promotion =
            type == ListingPromotionType.Bump
                ? ListingPromotion.CreateBump(
                    listing.Id,
                    pricing.Price,
                    debit.Transaction.Id,
                    now)
                : ListingPromotion.CreateVip(
                    listing.Id,
                    pricing.Price,
                    debit.Transaction.Id,
                    now,
                    endsAtUtc!.Value,
                    now);

        dbContext.ListingPromotions.Add(
            promotion);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await databaseTransaction.CommitAsync(
            cancellationToken);

        return new PurchaseListingPromotionResult(
            promotion.Id,
            listing.Id,
            (int)type,
            promotion.ChargedAmount,
            pricing.Currency,
            debit.Wallet.Balance,
            debit.Transaction.Id,
            promotion.StartsAtUtc,
            promotion.EndsAtUtc,
            false);
    }

    private async Task<PurchaseListingPromotionResult>
        GetExistingResultAsync(
            Listing listing,
            ListingPromotionType requestedType,
            WalletTransaction transaction,
            CancellationToken cancellationToken)
    {
        ListingPromotion? promotion =
            await dbContext.ListingPromotions
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.WalletTransactionId ==
                        transaction.Id,
                    cancellationToken);

        Wallet? wallet =
            await dbContext.Wallets
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.Id ==
                        transaction.WalletId &&
                        item.UserId ==
                        listing.OwnerId,
                    cancellationToken);

        if (promotion is null ||
            wallet is null ||
            promotion.ListingId != listing.Id ||
            promotion.Type != requestedType ||
            promotion.ChargedAmount !=
            transaction.Amount ||
            transaction.Direction !=
            WalletTransactionDirection.Debit ||
            transaction.Type !=
            WalletTransactionType.PromotionFee ||
            transaction.RelatedEntityId !=
            listing.Id)
        {
            throw new DomainException(
                "Idempotency key was already used for a different operation.");
        }

        return new PurchaseListingPromotionResult(
            promotion.Id,
            listing.Id,
            (int)promotion.Type,
            promotion.ChargedAmount,
            wallet.Currency,
            wallet.Balance,
            transaction.Id,
            promotion.StartsAtUtc,
            promotion.EndsAtUtc,
            true);
    }
}
