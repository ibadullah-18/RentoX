using RentoX.Domain.Auditing;
using RentoX.Application.Abstractions.Authentication;
using System.Data;
using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Common;
using RentoX.Application.Listings;
using RentoX.Application.Listings.Billing;
using RentoX.Application.Notifications;
using RentoX.Domain.Notifications;
using RentoX.Application.Wallets;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Billing;
using RentoX.Domain.Listings.Billing.Enums;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Wallets.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Listings;

public sealed class ListingModerationService(
    RentoXDbContext dbContext,
    IClock clock,
    IListingActivationPricingService pricingService,
    IWalletService walletService,
    INotificationService notificationService,
    ICurrentUserContext currentUserContext)
    : IListingModerationService
{
    private const int MaximumPageSize = 50;

    private static readonly TimeSpan ListingLifetime =
        TimeSpan.FromDays(30);

    public async Task<
        PagedResult<ModerationListingSummaryResult>>
        GetPendingAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        ValidatePagination(page, pageSize);

        IQueryable<Listing> query =
            dbContext.Listings
                .AsNoTracking()
                .Where(listing =>
                    listing.Status ==
                    ListingStatus.PendingReview);

        int totalCount =
            await query.CountAsync(
                cancellationToken);

        List<ModerationListingSummaryResult> items =
            await query
                .OrderBy(listing =>
                    listing.UpdatedAtUtc ??
                    listing.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(listing =>
                    new ModerationListingSummaryResult(
                        listing.Id,
                        listing.OwnerId,
                        listing.CategoryId,
                        listing.Title,
                        listing.Price,
                        listing.Currency,
                        (int)listing.RentalPeriodUnit,
                        listing.Images
                            .Where(image => image.IsCover)
                            .Select(image =>
                                (Guid?)image.Id)
                            .FirstOrDefault(),
                        listing.Images.Count,
                        listing.CreatedAtUtc,
                        listing.UpdatedAtUtc))
                .ToListAsync(cancellationToken);

        int totalPages =
            totalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    totalCount /
                    (double)pageSize);

        return new PagedResult<
            ModerationListingSummaryResult>(
                items,
                page,
                pageSize,
                totalCount,
                totalPages);
    }

    public async Task<ListingModerationResult>
        ApproveAsync(
            Guid listingId,
            CancellationToken cancellationToken = default)
    {
        Guid actorUserId = RequireAuditActor();
        Guid operationId = Guid.NewGuid();

        await using var databaseTransaction =
            await dbContext.Database
                .BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

        Listing listing =
            await GetListingAsync(
                listingId,
                cancellationToken);

        if (listing.Status is not
            (ListingStatus.PendingReview or
             ListingStatus.PaymentRequired))
        {
            throw new DomainException(
                "Only reviewed listings can be approved.");
        }

        ListingActivationPricingResult pricing =
            await pricingService.GetAsync(
                listing.OwnerId,
                listing.Id,
                cancellationToken);

        int cycleNumber =
            await GetNextCycleNumberAsync(
                listing.Id,
                cancellationToken);

        ListingStatus previousStatus = listing.Status;
        DateTimeOffset now = clock.UtcNow;
        DateTimeOffset periodEndUtc =
            now.Add(ListingLifetime);

        if (!pricing.RequiresPayment)
        {
            listing.Publish(
                now,
                ListingLifetime);

            RecordListingStatusChange(
                listing, previousStatus, actorUserId, operationId);

            ListingBillingCycle freeCycle =
                ListingBillingCycle.CreateFree(
                    listing.Id,
                    cycleNumber,
                    ListingBillingCycleType
                        .InitialActivation,
                    now,
                    periodEndUtc,
                    now);

            dbContext.ListingBillingCycles.Add(
                freeCycle);

            await dbContext.SaveChangesAsync(
                cancellationToken);

            await databaseTransaction.CommitAsync(
                cancellationToken);

            await notificationService.CreateAsync(
                new CreateNotificationCommand(
                    listing.OwnerId,
                    (int)NotificationType.ListingApproved,
                    "Elan təsdiqləndi",
                    $"\"{listing.Title}\" elanı yayımlandı.",
                    listing.Id,
                    $"/listings/{listing.Id:D}"),
                cancellationToken);

            return CreateResult(
                listing,
                false,
                0,
                0,
                null);
        }

        WalletBalanceResult wallet =
            await walletService.GetAsync(
                listing.OwnerId,
                cancellationToken);

        if (wallet.Balance <
            pricing.ActivationFee)
        {
            if (listing.Status ==
                ListingStatus.PendingReview)
            {
                listing.RequirePayment();

                RecordListingStatusChange(
                    listing, previousStatus, actorUserId, operationId);

                await dbContext.SaveChangesAsync(
                    cancellationToken);
            }

            await databaseTransaction.CommitAsync(
                cancellationToken);

            await notificationService.CreateAsync(
                new CreateNotificationCommand(
                    listing.OwnerId,
                    (int)NotificationType.ListingPaymentRequired,
                    "Ödəniş tələb olunur",
                    $"\"{listing.Title}\" elanını aktivləşdirmək üçün balans ödənişi tələb olunur.",
                    listing.Id,
                    $"/listings/{listing.Id:D}"),
                cancellationToken);

            return CreateResult(
                listing,
                true,
                pricing.ActivationFee,
                0,
                null);
        }

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

        listing.Publish(
            now,
            ListingLifetime);

        RecordListingStatusChange(
            listing, previousStatus, actorUserId, operationId);

        ListingBillingCycle paidCycle =
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
            paidCycle);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await databaseTransaction.CommitAsync(
            cancellationToken);

        await notificationService.CreateAsync(
            new CreateNotificationCommand(
                listing.OwnerId,
                (int)NotificationType.ListingApproved,
                "Elan təsdiqləndi",
                $"\"{listing.Title}\" elanı yayımlandı.",
                listing.Id,
                $"/listings/{listing.Id:D}"),
            cancellationToken);

        return CreateResult(
            listing,
            false,
            pricing.ActivationFee,
            pricing.ActivationFee,
            debitResult.Transaction.Id);
    }

    public async Task<ListingModerationResult>
        RejectAsync(
            Guid listingId,
            string reason,
            CancellationToken cancellationToken = default)
    {
        Guid actorUserId = RequireAuditActor();
        Guid operationId = Guid.NewGuid();

        await using var auditTransaction =
            await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        Listing listing =
            await GetListingAsync(
                listingId,
                cancellationToken);

        ListingStatus previousStatus = listing.Status;

        listing.Reject(reason);

        RecordListingStatusChange(
            listing, previousStatus, actorUserId, operationId);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await auditTransaction.CommitAsync(cancellationToken);

        await notificationService.CreateAsync(
            new CreateNotificationCommand(
                listing.OwnerId,
                (int)NotificationType.ListingRejected,
                "Elan rədd edildi",
                $"\"{listing.Title}\" elanı rədd edildi. Səbəb: {reason.Trim()}",
                listing.Id,
                $"/listings/{listing.Id:D}"),
            cancellationToken);

        return CreateResult(
            listing,
            false,
            0,
            0,
            null);
    }

    private Guid RequireAuditActor()
    {
        Guid? userId = currentUserContext.UserId;

        if (!currentUserContext.IsAuthenticated ||
            !userId.HasValue ||
            userId.Value == Guid.Empty)
        {
            throw new DomainException(
                "An authenticated audit actor is required.");
        }

        return userId.Value;
    }

    private void RecordListingStatusChange(
        Listing listing,
        ListingStatus previousStatus,
        Guid actorUserId,
        Guid operationId)
    {
        if (previousStatus == listing.Status)
        {
            return;
        }

        dbContext.Set<AuditLogEntry>().Add(
            AuditLogEntry.ForListingStatusChange(
                operationId,
                actorUserId,
                listing.Id,
                previousStatus,
                listing.Status,
                clock.UtcNow));
    }

    private async Task<Listing> GetListingAsync(
        Guid listingId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Listings
            .SingleOrDefaultAsync(
                listing => listing.Id == listingId,
                cancellationToken)
            ?? throw new DomainException(
                "Listing was not found.");
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

        return maximumCycleNumber.GetValueOrDefault() + 1;
    }

    private static ListingModerationResult CreateResult(
        Listing listing,
        bool requiresPayment,
        decimal activationFee,
        decimal chargedAmount,
        Guid? walletTransactionId)
    {
        return new ListingModerationResult(
            listing.Id,
            (int)listing.Status,
            listing.RejectionReason,
            listing.PublishedAtUtc,
            listing.ExpiresAtUtc,
            listing.UpdatedAtUtc,
            requiresPayment,
            activationFee,
            chargedAmount,
            walletTransactionId);
    }

    private static void ValidatePagination(
        int page,
        int pageSize)
    {
        if (page <= 0)
        {
            throw new DomainException(
                "Page must be greater than zero.");
        }

        if (pageSize <= 0 ||
            pageSize > MaximumPageSize)
        {
            throw new DomainException(
                "Page size must be between 1 and 50.");
        }
    }
}
