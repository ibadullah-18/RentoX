using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Common;
using RentoX.Application.Listings;
using RentoX.Application.Listings.Search;
using RentoX.Application.Stores;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Stores;
using RentoX.Domain.Stores.Enums;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Persistence;
using RentoX.Infrastructure.Stores.Search;

namespace RentoX.Infrastructure.Stores;

public sealed class PublicStoreQueryService(
    RentoXDbContext dbContext,
    IClock clock,
    IPublicListingQueryService listingQueryService)
    : IPublicStoreQueryService
{
    private const int MaximumSlugLength = 120;
    private const int MaximumPageSize = 50;

    public async Task<PublicStoreDetailsResult?>
        GetBySlugAsync(
            string slug,
            CancellationToken cancellationToken = default)
    {
        string normalizedSlug = NormalizeSlug(slug);

        StoreProfile? store =
            await dbContext.Set<StoreProfile>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.Slug == normalizedSlug &&
                        item.Status == StoreStatus.Active,
                    cancellationToken);

        if (store is null)
        {
            return null;
        }

        DateTimeOffset now = clock.UtcNow;

        IQueryable<Listing> activeListings =
            dbContext.Listings
                .AsNoTracking()
                .Where(listing =>
                    listing.OwnerId == store.OwnerId &&
                    listing.Status == ListingStatus.Active &&
                    listing.ExpiresAtUtc.HasValue &&
                    listing.ExpiresAtUtc.Value > now);

        long activeListingCount =
            await activeListings.LongCountAsync(
                cancellationToken);

        long totalViewCount =
            await activeListings
                .Select(listing =>
                    (long?)listing.ViewCount)
                .SumAsync(cancellationToken)
            ?? 0;

        IQueryable<Guid> activeListingIds =
            activeListings.Select(listing => listing.Id);

        long totalFavoriteCount =
            await dbContext.Favorites
                .AsNoTracking()
                .LongCountAsync(
                    favorite =>
                        activeListingIds.Contains(
                            favorite.ListingId),
                    cancellationToken);

        DateTimeOffset imageChangedAt =
            store.UpdatedAtUtc ??
            store.CreatedAtUtc;

        return new PublicStoreDetailsResult(
            store.Id,
            store.Name,
            store.Slug,
            store.Description,
            store.PhoneNumber,
            store.Email,
            store.Address,
            store.InstagramUrl,
            store.TiktokUrl,
            store.FacebookUrl,
            store.WebsiteUrl,
            !string.IsNullOrWhiteSpace(
                store.LogoImageKey),
            !string.IsNullOrWhiteSpace(
                store.CoverImageKey),
            imageChangedAt.ToUnixTimeMilliseconds(),
            activeListingCount,
            totalViewCount,
            totalFavoriteCount,
            store.CreatedAtUtc);
    }

    public async Task<PagedResult<PublicStoreSummaryResult>>
        SearchAsync(
            PublicStoreSearchQuery query,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentOutOfRangeException.ThrowIfLessThan(query.Page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(query.PageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            query.PageSize,
            MaximumPageSize);

        DateTimeOffset now = clock.UtcNow;

        IQueryable<StoreProfile> stores =
            dbContext.Set<StoreProfile>()
                .AsNoTracking()
                .Where(store => store.Status == StoreStatus.Active);

        string? search =
            string.IsNullOrWhiteSpace(query.Search)
                ? null
                : query.Search.Trim();

        IReadOnlyList<StoreSearchScore>? scores = null;

        if (search is not null)
        {
            scores = await StoreSearchScorer.ScoreAsync(
                dbContext,
                ListingSearchTerms.Parse(search),
                cancellationToken);

            if (scores is null)
            {
                // Database not upgraded for smart search: plain matching.
                string pattern = $"%{search}%";

                stores = stores.Where(store =>
                    EF.Functions.ILike(store.Name, pattern) ||
                    (store.Description != null &&
                     EF.Functions.ILike(store.Description, pattern)));
            }
            else
            {
                Guid[] candidateIds =
                    scores.Select(score => score.Id).ToArray();

                stores = stores.Where(store =>
                    candidateIds.Contains(store.Id));
            }
        }

        var projected = stores.Select(store => new
        {
            store.Id,
            store.Name,
            store.Slug,
            store.Description,
            store.LogoImageKey,
            store.CreatedAtUtc,
            store.UpdatedAtUtc,
            ListingCount =
                dbContext.Listings.Count(listing =>
                    listing.OwnerId == store.OwnerId &&
                    listing.Status == ListingStatus.Active &&
                    listing.ExpiresAtUtc.HasValue &&
                    listing.ExpiresAtUtc.Value > now),
            FollowerCount =
                dbContext.StoreFollowers.Count(follower =>
                    follower.StoreId == store.Id)
        });

        int totalCount = await projected.CountAsync(cancellationToken);

        var rows = scores is { Count: > 0 }
            ? await projected.ToListAsync(cancellationToken)
            : await projected
                .OrderByDescending(row => row.ListingCount)
                .ThenByDescending(row => row.FollowerCount)
                .ThenBy(row => row.Name)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

        if (scores is { Count: > 0 })
        {
            Dictionary<Guid, double> scoreById =
                scores.ToDictionary(score => score.Id, score => score.Score);

            rows =
                rows
                    .OrderByDescending(row => scoreById[row.Id])
                    .ThenByDescending(row => row.ListingCount)
                    .ThenByDescending(row => row.FollowerCount)
                    .ThenBy(row => row.Name)
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToList();
        }

        PublicStoreSummaryResult[] items =
            rows
                .Select(row => new PublicStoreSummaryResult(
                    row.Id,
                    row.Name,
                    row.Slug,
                    row.Description,
                    !string.IsNullOrWhiteSpace(row.LogoImageKey),
                    (row.UpdatedAtUtc ?? row.CreatedAtUtc)
                        .ToUnixTimeMilliseconds(),
                    row.ListingCount,
                    row.FollowerCount))
                .ToArray();

        int totalPages =
            totalCount == 0
                ? 0
                : (int)Math.Ceiling(totalCount / (double)query.PageSize);

        return new PagedResult<PublicStoreSummaryResult>(
            items,
            query.Page,
            query.PageSize,
            totalCount,
            totalPages);
    }

    public async Task<
        PagedResult<PublicListingSummaryResult>?>
        GetListingsAsync(
            string slug,
            PreferredLanguage language,
            Guid? viewerUserId,
            PublicListingSearchQuery searchQuery,
            CancellationToken cancellationToken = default)
    {
        string normalizedSlug = NormalizeSlug(slug);

        Guid? ownerId =
            await dbContext.Set<StoreProfile>()
                .AsNoTracking()
                .Where(store =>
                    store.Slug == normalizedSlug &&
                    store.Status == StoreStatus.Active)
                .Select(store =>
                    (Guid?)store.OwnerId)
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (!ownerId.HasValue)
        {
            return null;
        }

        PublicListingSearchQuery ownerQuery =
            searchQuery with
            {
                OwnerId = ownerId.Value
            };

        return await listingQueryService.SearchAsync(
            ownerQuery,
            language,
            viewerUserId,
            cancellationToken);
    }

    private static string NormalizeSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new DomainException(
                "Store slug is required.");
        }

        string normalized =
            slug.Trim().ToLowerInvariant();

        if (normalized.Length > MaximumSlugLength)
        {
            throw new DomainException(
                "Store slug cannot exceed 120 characters.");
        }

        return normalized;
    }
}