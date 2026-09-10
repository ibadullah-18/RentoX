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

public sealed class ListingActivationPaymentService(
    RentoXDbContext dbContext,
    IClock clock,
    IListingActivationPricingService pricingService,
    IWalletService walletService)
    : IListingActivationPaymentService
{
    private static readonly TimeSpan ListingLifetime =
        TimeSpan.FromDays(30);

    public async Task<ListingActivationPaymentResult>
        PayAsync(
            PayListingActivationCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.OwnerId == Guid.Empty)
        {
            throw new DomainException(
                "Listing owner id is required.");
        }

        if (command.ListingId == Guid.Empty)
        {
            throw new DomainException(
                "Listing id is required.");
        }

        await using var databaseTransaction =
            await dbContext.Database
                .BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

        Listing listing =
            await dbContext.Listings
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == command.ListingId &&
                        item.OwnerId == command.OwnerId,
                    cancellationToken)
            ?? throw new DomainException(
                "Listing was not found.");

        if (listing.Status == ListingStatus.Active)
        {
            ListingActivationPaymentResult?
                existingResult =
                    await GetExistingPaymentResultAsync(
                        listing,
                        cancellationToken);

            if (existingResult is null)
            {
                throw new DomainException(
                    "The listing is already active and does not require payment.");
            }

            await databaseTransaction.CommitAsync(
                cancellationToken);

            return existingResult;
        }

        if (listing.Status !=
            ListingStatus.PaymentRequired)
        {
            throw new DomainException(
                "Only listings awaiting payment can be paid.");
        }

        ListingActivationPricingResult pricing =
            await pricingService.GetAsync(
                listing.OwnerId,
                listing.Id,
                cancellationToken);

        if (!pricing.RequiresPayment ||
            pricing.ActivationFee <= 0)
        {
            throw new DomainException(
                "The listing does not require an activation payment.");
        }

        int cycleNumber =
            await GetNextCycleNumberAsync(
                listing.Id,
                cancellationToken);

        string idempotencyKey =
            $"listing-activation:{listing.Id}:cycle:{cycleNumber}";

        WalletOperationResult debitResult =
            await walletService.DebitAsync(
                new DebitWalletCommand(
                    listing.OwnerId,
                    pricing.ActivationFee,
                    WalletTransactionType.ListingFee,
                    "Listing activation fee",
                    listing.Id,
                    idempotencyKey),
                cancellationToken);

        DateTimeOffset now = clock.UtcNow;
        DateTimeOffset periodEndUtc =
            now.Add(ListingLifetime);

        listing.Publish(
            now,
            ListingLifetime);

        ListingBillingCycle billingCycle =
            ListingBillingCycle.CreatePaid(
                listing.Id,
                cycleNumber,
                ListingBillingCycleType
                    .InitialActivation,
                now,
                periodEndUtc,
                pricing.ActivationFee,
                debitResult.Transaction.Id,
                now);

        dbContext.ListingBillingCycles.Add(
            billingCycle);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await databaseTransaction.CommitAsync(
            cancellationToken);

        return new ListingActivationPaymentResult(
            listing.Id,
            (int)listing.Status,
            pricing.ActivationFee,
            debitResult.Wallet.Balance,
            debitResult.Transaction.Id,
            now,
            periodEndUtc,
            debitResult.WasAlreadyProcessed);
    }

    private async Task<ListingActivationPaymentResult?>
        GetExistingPaymentResultAsync(
            Listing listing,
            CancellationToken cancellationToken)
    {
        ListingBillingCycle? billingCycle =
            await dbContext.ListingBillingCycles
                .AsNoTracking()
                .Where(cycle =>
                    cycle.ListingId == listing.Id &&
                    cycle.WalletTransactionId.HasValue &&
                    !cycle.RefundedAtUtc.HasValue)
                .OrderByDescending(cycle =>
                    cycle.CycleNumber)
                .FirstOrDefaultAsync(
                    cancellationToken);

        if (billingCycle is null ||
            !billingCycle.WalletTransactionId.HasValue)
        {
            return null;
        }

        Guid walletTransactionId =
            billingCycle.WalletTransactionId.Value;

        WalletTransaction walletTransaction =
            await dbContext.WalletTransactions
                .AsNoTracking()
                .SingleAsync(
                    transaction =>
                        transaction.Id ==
                        walletTransactionId,
                    cancellationToken);

        if (!listing.PublishedAtUtc.HasValue ||
            !listing.ExpiresAtUtc.HasValue)
        {
            throw new DomainException(
                "Active listing period is invalid.");
        }

        return new ListingActivationPaymentResult(
            listing.Id,
            (int)listing.Status,
            billingCycle.ChargedAmount,
            walletTransaction.BalanceAfter,
            walletTransaction.Id,
            listing.PublishedAtUtc.Value,
            listing.ExpiresAtUtc.Value,
            true);
    }

    private async Task<int> GetNextCycleNumberAsync(
        Guid listingId,
        CancellationToken cancellationToken)
    {
        int? maximumCycleNumber =
            await dbContext.ListingBillingCycles
                .Where(cycle =>
                    cycle.ListingId == listingId)
                .MaxAsync(
                    cycle =>
                        (int?)cycle.CycleNumber,
                    cancellationToken);

        return maximumCycleNumber
            .GetValueOrDefault() + 1;
    }
}
