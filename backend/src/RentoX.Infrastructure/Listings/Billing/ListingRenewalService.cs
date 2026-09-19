using System.Data;
using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Listings.Billing;
using RentoX.Application.Wallets;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Billing;
using RentoX.Domain.Listings.Billing.Enums;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Wallets;
using RentoX.Domain.Wallets.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Listings.Billing;

public sealed class ListingRenewalService(
    RentoXDbContext dbContext,
    IClock clock,
    IListingActivationPricingService pricingService,
    IWalletService walletService)
    : IListingRenewalService
{
    private static readonly TimeSpan ListingLifetime =
        TimeSpan.FromDays(30);

    public async Task<ListingRenewalResult> RenewAsync(
        Guid ownerId,
        Guid listingId,
        CancellationToken cancellationToken = default)
    {
        if (ownerId == Guid.Empty ||
            listingId == Guid.Empty)
        {
            throw new DomainException(
                "Listing owner and listing id are required.");
        }

        await using var databaseTransaction =
            await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        DateTimeOffset now = clock.UtcNow;

        Listing listing =
            await dbContext.Listings.SingleOrDefaultAsync(
                item =>
                    item.Id == listingId &&
                    item.OwnerId == ownerId,
                cancellationToken)
            ?? throw new DomainException(
                "Listing was not found.");

        if (listing.Status == ListingStatus.Active &&
            listing.ExpiresAtUtc.HasValue &&
            listing.ExpiresAtUtc.Value > now)
        {
            ListingBillingCycle? latestCycle =
                await dbContext.ListingBillingCycles
                    .AsNoTracking()
                    .Where(cycle =>
                        cycle.ListingId == listing.Id)
                    .OrderByDescending(cycle =>
                        cycle.CycleNumber)
                    .FirstOrDefaultAsync(
                        cancellationToken);

            if (latestCycle is null ||
                latestCycle.Type !=
                    ListingBillingCycleType.Renewal ||
                listing.PublishedAtUtc !=
                    latestCycle.PeriodStartUtc ||
                listing.ExpiresAtUtc !=
                    latestCycle.PeriodEndUtc)
            {
                throw new DomainException(
                    "The listing has not expired.");
            }

            ListingRenewalResult existingResult =
                await GetExistingResultAsync(
                    listing,
                    latestCycle,
                    cancellationToken);

            await databaseTransaction.CommitAsync(
                cancellationToken);

            return existingResult;
        }

        if (listing.Status is not
            (ListingStatus.Expired or
             ListingStatus.Active or
             ListingStatus.Deactivated) ||
            !listing.ExpiresAtUtc.HasValue ||
            listing.ExpiresAtUtc.Value > now)
        {
            throw new DomainException(
                "Only expired listings can be renewed.");
        }

        ListingActivationPricingResult pricing =
            await pricingService.GetAsync(
                ownerId,
                listingId,
                cancellationToken);

        int? previousCycleNumber =
            await dbContext.ListingBillingCycles
                .Where(cycle =>
                    cycle.ListingId == listingId)
                .MaxAsync(
                    cycle => (int?)cycle.CycleNumber,
                    cancellationToken);

        int cycleNumber =
            previousCycleNumber.GetValueOrDefault() + 1;

        WalletBalanceResult wallet =
            await walletService.GetAsync(
                ownerId,
                cancellationToken);

        if (pricing.RequiresPayment &&
            wallet.Balance < pricing.ActivationFee)
        {
            throw new DomainException(
                "Insufficient wallet balance for listing renewal.");
        }

        DateTimeOffset periodEndUtc =
            now.Add(ListingLifetime);

        if (!pricing.RequiresPayment)
        {
            listing.Renew(now, ListingLifetime);

            dbContext.ListingBillingCycles.Add(
                ListingBillingCycle.CreateFree(
                    listingId,
                    cycleNumber,
                    ListingBillingCycleType.Renewal,
                    now,
                    periodEndUtc,
                    now));

            await dbContext.SaveChangesAsync(
                cancellationToken);

            await databaseTransaction.CommitAsync(
                cancellationToken);

            return new ListingRenewalResult(
                listingId,
                (int)listing.Status,
                cycleNumber,
                0,
                wallet.Balance,
                null,
                now,
                periodEndUtc,
                false);
        }

        if (pricing.ActivationFee <= 0)
        {
            throw new DomainException(
                "Listing renewal fee is invalid.");
        }

        string idempotencyKey =
            $"listing-renewal:{listingId}:cycle:{cycleNumber}";

        WalletOperationResult debitResult =
            await walletService.DebitAsync(
                new DebitWalletCommand(
                    ownerId,
                    pricing.ActivationFee,
                    WalletTransactionType.ListingFee,
                    "Listing renewal fee",
                    listingId,
                    idempotencyKey),
                cancellationToken);

        listing.Renew(now, ListingLifetime);

        dbContext.ListingBillingCycles.Add(
            ListingBillingCycle.CreatePaid(
                listingId,
                cycleNumber,
                ListingBillingCycleType.Renewal,
                now,
                periodEndUtc,
                pricing.ActivationFee,
                debitResult.Transaction.Id,
                now));

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await databaseTransaction.CommitAsync(
            cancellationToken);

        return new ListingRenewalResult(
            listingId,
            (int)listing.Status,
            cycleNumber,
            pricing.ActivationFee,
            debitResult.Wallet.Balance,
            debitResult.Transaction.Id,
            now,
            periodEndUtc,
            debitResult.WasAlreadyProcessed);
    }

    private async Task<ListingRenewalResult>
        GetExistingResultAsync(
            Listing listing,
            ListingBillingCycle cycle,
            CancellationToken cancellationToken)
    {
        if (cycle.WalletTransactionId.HasValue)
        {
            WalletTransaction transaction =
                await dbContext.WalletTransactions
                    .AsNoTracking()
                    .SingleAsync(
                        item =>
                            item.Id ==
                            cycle.WalletTransactionId.Value,
                        cancellationToken);

            return new ListingRenewalResult(
                listing.Id,
                (int)listing.Status,
                cycle.CycleNumber,
                cycle.ChargedAmount,
                transaction.BalanceAfter,
                transaction.Id,
                cycle.PeriodStartUtc,
                cycle.PeriodEndUtc,
                true);
        }

        WalletBalanceResult wallet =
            await walletService.GetAsync(
                listing.OwnerId,
                cancellationToken);

        return new ListingRenewalResult(
            listing.Id,
            (int)listing.Status,
            cycle.CycleNumber,
            0,
            wallet.Balance,
            null,
            cycle.PeriodStartUtc,
            cycle.PeriodEndUtc,
            true);
    }
}
