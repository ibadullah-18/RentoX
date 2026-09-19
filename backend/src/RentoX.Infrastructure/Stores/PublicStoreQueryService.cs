using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Common;
using RentoX.Application.Listings;
using RentoX.Application.Stores;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Stores;
using RentoX.Domain.Stores.Enums;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Stores;

public sealed class PublicStoreQueryService(
    RentoXDbContext dbContext,
    IClock clock,
    IPublicListingQueryService listingQueryService)
    : IPublicStoreQueryService
{
    private const int MaximumSlugLength = 120;

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