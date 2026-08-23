using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Common;
using RentoX.Application.Listings;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Listings;

public sealed class ListingModerationService(
    RentoXDbContext dbContext,
    IClock clock)
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
        Listing listing =
            await GetListingAsync(
                listingId,
                cancellationToken);

        listing.Publish(
            clock.UtcNow,
            ListingLifetime);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return CreateResult(listing);
    }

    public async Task<ListingModerationResult>
        RejectAsync(
            Guid listingId,
            string reason,
            CancellationToken cancellationToken = default)
    {
        Listing listing =
            await GetListingAsync(
                listingId,
                cancellationToken);

        listing.Reject(reason);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return CreateResult(listing);
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

    private static ListingModerationResult CreateResult(
        Listing listing)
    {
        return new ListingModerationResult(
            listing.Id,
            (int)listing.Status,
            listing.RejectionReason,
            listing.PublishedAtUtc,
            listing.ExpiresAtUtc,
            listing.UpdatedAtUtc);
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