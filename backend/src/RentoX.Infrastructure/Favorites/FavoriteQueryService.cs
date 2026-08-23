using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Common;
using RentoX.Application.Favorites;
using RentoX.Application.Listings;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Favorites;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Favorites;

public sealed class FavoriteQueryService(
    RentoXDbContext dbContext,
    IClock clock)
    : IFavoriteQueryService
{
    private const int MaximumPageSize = 50;

    public async Task<
        PagedResult<PublicListingSummaryResult>>
        GetMineAsync(
            Guid userId,
            PreferredLanguage language,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        ValidatePagination(
            page,
            pageSize);

        DateTimeOffset now = clock.UtcNow;

        IQueryable<FavoriteListingProjection> query =
            from favorite in
                dbContext.Favorites.AsNoTracking()
            join listing in
                dbContext.Listings.AsNoTracking()
                on favorite.ListingId equals listing.Id
            where
                favorite.UserId == userId &&
                listing.Status == ListingStatus.Active &&
                listing.ExpiresAtUtc.HasValue &&
                listing.ExpiresAtUtc.Value > now
            orderby favorite.CreatedAtUtc descending
            select new FavoriteListingProjection(
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
                listing.ViewCount,
                dbContext.Favorites.Count(item =>
                    item.ListingId == listing.Id),
                listing.PublishedAtUtc!.Value,
                listing.ExpiresAtUtc!.Value,
                favorite.CreatedAtUtc);

        int totalCount =
            await query.CountAsync(
                cancellationToken);

        List<FavoriteListingProjection> projections =
            await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

        Guid[] categoryIds =
            projections
                .Select(item => item.CategoryId)
                .Distinct()
                .ToArray();

        List<Category> categories =
            await dbContext.Categories
                .AsNoTracking()
                .Include(category =>
                    category.Translations)
                .Where(category =>
                    categoryIds.Contains(category.Id))
                .ToListAsync(cancellationToken);

        Dictionary<Guid, string> categoryNames =
            categories.ToDictionary(
                category => category.Id,
                category => GetCategoryName(
                    category,
                    language));

        List<PublicListingSummaryResult> items =
            projections
                .Select(item =>
                    new PublicListingSummaryResult(
                        item.Id,
                        item.OwnerId,
                        item.CategoryId,
                        categoryNames.GetValueOrDefault(
                            item.CategoryId,
                            string.Empty),
                        item.Title,
                        item.Price,
                        item.Currency,
                        item.RentalPeriodUnit,
                        item.CoverImageId,
                        item.ViewCount,
                        item.FavoriteCount,
                        true,
                        item.PublishedAtUtc,
                        item.ExpiresAtUtc))
                .ToList();

        int totalPages =
            totalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    totalCount /
                    (double)pageSize);

        return new PagedResult<
            PublicListingSummaryResult>(
                items,
                page,
                pageSize,
                totalCount,
                totalPages);
    }

    private static string GetCategoryName(
        Category category,
        PreferredLanguage language)
    {
        CategoryTranslation? translation =
            category.Translations.FirstOrDefault(item =>
                item.Language == language)
            ?? category.Translations.FirstOrDefault(item =>
                item.Language ==
                PreferredLanguage.Azerbaijani)
            ?? category.Translations.FirstOrDefault();

        return translation?.Name ?? category.Slug;
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

    private sealed record FavoriteListingProjection(
        Guid Id,
        Guid OwnerId,
        Guid CategoryId,
        string Title,
        decimal Price,
        string Currency,
        int RentalPeriodUnit,
        Guid? CoverImageId,
        long ViewCount,
        int FavoriteCount,
        DateTimeOffset PublishedAtUtc,
        DateTimeOffset ExpiresAtUtc,
        DateTimeOffset FavoritedAtUtc);
}