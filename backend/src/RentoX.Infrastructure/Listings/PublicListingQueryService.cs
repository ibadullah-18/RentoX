using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Common;
using RentoX.Application.Listings;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Catalog.Fields;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Listings.Promotions.Enums;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Listings;

public sealed class PublicListingQueryService(
    RentoXDbContext dbContext,
    IClock clock,
    IListingViewRecorder viewRecorder)
    : IPublicListingQueryService
{
    private const int MaximumPageSize = 50;
    private const int MaximumSearchLength = 100;

    public async Task<
        PagedResult<PublicListingSummaryResult>>
        SearchAsync(
            PublicListingSearchQuery query,
            PreferredLanguage language,
            Guid? viewerUserId,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        ValidatePagination(
            query.Page,
            query.PageSize);

        if ((query.MinPrice.HasValue &&
             query.MinPrice.Value < 0) ||
            (query.MaxPrice.HasValue &&
             query.MaxPrice.Value < 0))
        {
            throw new DomainException(
                "Price cannot be negative.");
        }

        if (query.MinPrice.HasValue &&
            query.MaxPrice.HasValue &&
            query.MinPrice.Value >
                query.MaxPrice.Value)
        {
            throw new DomainException(
                "Minimum price cannot exceed maximum price.");
        }
        string? search =
            NormalizeSearch(query.Search);

        List<Category> categories =
            await dbContext.Categories
                .AsNoTracking()
                .Include(category =>
                    category.Translations)
                .Where(category =>
                    category.IsActive)
                .ToListAsync(cancellationToken);

        Guid[] allowedCategoryIds =
            GetAllowedCategoryIds(
                query.CategoryId,
                categories);

        DateTimeOffset now = clock.UtcNow;

        IQueryable<Listing> listingQuery =
            dbContext.Listings
                .AsNoTracking()
                .Where(listing =>
                    listing.Status ==
                    ListingStatus.Active &&
                    listing.ExpiresAtUtc.HasValue &&
                    listing.ExpiresAtUtc > now);

        if (query.OwnerId.HasValue)
        {
            listingQuery =
                listingQuery.Where(listing =>
                    listing.OwnerId ==
                    query.OwnerId.Value);
        }

        if (query.CategoryId.HasValue)
        {
            listingQuery =
                listingQuery.Where(listing =>
                    allowedCategoryIds.Contains(
                        listing.CategoryId));
        }

        if (search is not null)
        {
            string searchPattern = $"%{search}%";

            listingQuery =
                listingQuery.Where(listing =>
                    EF.Functions.ILike(
                        listing.Title,
                        searchPattern) ||
                    EF.Functions.ILike(
                        listing.Description,
                        searchPattern));
        }

        if (query.MinPrice is decimal minPrice)
        {
            listingQuery =
                listingQuery.Where(listing =>
                    listing.Price >= minPrice);
        }

        if (query.MaxPrice is decimal maxPrice)
        {
            listingQuery =
                listingQuery.Where(listing =>
                    listing.Price <= maxPrice);
        }
        Guid[] requestedOptionIds =
            query.OptionIds?.Distinct().ToArray() ?? [];

        if (query.OptionId.HasValue)
        {
            if (requestedOptionIds.Length > 0)
            {
                throw new DomainException(
                    "Use either optionId or optionIds.");
            }

            requestedOptionIds = [query.OptionId.Value];
        }

        if (requestedOptionIds.Length > 0)
        {
            var validOptions =
                await (
                    from option in dbContext.CategoryFieldOptions
                        .AsNoTracking()
                    join field in dbContext.CategoryFields
                        .AsNoTracking()
                        on option.CategoryFieldId equals field.Id
                    where requestedOptionIds.Contains(option.Id) &&
                          option.IsActive &&
                          field.IsActive &&
                          field.IsFilterable &&
                          (field.Type ==
                              CategoryFieldType.SingleSelect ||
                           field.Type ==
                              CategoryFieldType.MultiSelect)
                    select new
                    {
                        option.Id,
                        FieldId = field.Id
                    }
                ).ToListAsync(cancellationToken);

            if (validOptions.Count != requestedOptionIds.Length)
            {
                throw new DomainException(
                    "A filter option was not found or is not filterable.");
            }

            Guid[] fieldIds =
                validOptions
                    .Select(option => option.FieldId)
                    .Distinct()
                    .ToArray();

            if (fieldIds.Length != 1)
            {
                throw new DomainException(
                    "All filter options must belong to the same field.");
            }

            Guid fieldId = fieldIds[0];

            listingQuery =
                listingQuery.Where(listing =>
                    listing.FieldValues.Any(value =>
                        value.CategoryFieldId == fieldId &&
                        value.Selections.Any(selection =>
                            requestedOptionIds.Contains(
                                selection.CategoryFieldOptionId))));
        }

        bool hasNumericBound =
            query.NumericMin.HasValue ||
            query.NumericMax.HasValue;

        if (query.NumericFieldId.HasValue != hasNumericBound)
        {
            throw new DomainException(
                "Numeric field id and at least one numeric bound are required together.");
        }

        if (query.NumericMin.HasValue &&
            query.NumericMax.HasValue &&
            query.NumericMin.Value > query.NumericMax.Value)
        {
            throw new DomainException(
                "Numeric minimum cannot exceed maximum.");
        }

        if (query.NumericFieldId.HasValue)
        {
            Guid numericFieldId = query.NumericFieldId.Value;

            bool validNumericField =
                await dbContext.CategoryFields
                    .AsNoTracking()
                    .AnyAsync(
                        field =>
                            field.Id == numericFieldId &&
                            field.IsActive &&
                            field.IsFilterable &&
                            (field.Type == (CategoryFieldType)2 ||
                             field.Type == (CategoryFieldType)3),
                        cancellationToken);

            if (!validNumericField)
            {
                throw new DomainException(
                    "Numeric filter field was not found or is not filterable.");
            }

            decimal? minimum = query.NumericMin;
            decimal? maximum = query.NumericMax;

            listingQuery =
                listingQuery.Where(listing =>
                    listing.FieldValues.Any(value =>
                        value.CategoryFieldId == numericFieldId &&
                        value.NumericValue.HasValue &&
                        (!minimum.HasValue ||
                         value.NumericValue >= minimum) &&
                        (!maximum.HasValue ||
                         value.NumericValue <= maximum)));
        }

        int totalCount =
            await listingQuery.CountAsync(
                cancellationToken);

        List<ListingProjection> projections =
            await listingQuery
                .OrderByDescending(listing =>
                    dbContext.ListingPromotions.Any(promotion =>
                        promotion.ListingId == listing.Id &&
                        promotion.Type == ListingPromotionType.Vip &&
                        promotion.StartsAtUtc <= now &&
                        promotion.EndsAtUtc > now))
                .ThenByDescending(listing =>
                    dbContext.ListingPromotions
                        .Where(promotion =>
                            promotion.ListingId == listing.Id &&
                            promotion.Type == ListingPromotionType.Bump &&
                            promotion.StartsAtUtc >= listing.PublishedAtUtc)
                        .Max(promotion =>
                            (DateTimeOffset?)promotion.StartsAtUtc)
                    ?? listing.PublishedAtUtc)
                .ThenByDescending(listing =>
                    listing.Id)
                .Skip(
                    (query.Page - 1) *
                    query.PageSize)
                .Take(query.PageSize)
                .Select(listing =>
                new ListingProjection(
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
                    listing.PublishedAtUtc!.Value,
                    listing.ExpiresAtUtc!.Value))
                .ToListAsync(cancellationToken);

        Guid[] projectedListingIds =
    projections
        .Select(item => item.Id)
        .ToArray();

        Dictionary<Guid, int> favoriteCounts = [];

        if (projectedListingIds.Length > 0)
        {
            favoriteCounts =
                await dbContext.Favorites
                    .AsNoTracking()
                    .Where(favorite =>
                        projectedListingIds.Contains(
                            favorite.ListingId))
                    .GroupBy(favorite =>
                        favorite.ListingId)
                    .Select(group => new
                    {
                        ListingId = group.Key,
                        Count = group.Count()
                    })
                    .ToDictionaryAsync(
                        item => item.ListingId,
                        item => item.Count,
                        cancellationToken);
        }

        HashSet<Guid> viewerFavoriteIds = [];

        if (viewerUserId.HasValue &&
            projectedListingIds.Length > 0)
        {
            List<Guid> favoriteIds =
                await dbContext.Favorites
                    .AsNoTracking()
                    .Where(favorite =>
                        favorite.UserId ==
                            viewerUserId.Value &&
                        projectedListingIds.Contains(
                            favorite.ListingId))
                    .Select(favorite =>
                        favorite.ListingId)
                    .ToListAsync(cancellationToken);

            viewerFavoriteIds =
                favoriteIds.ToHashSet();
        }

        HashSet<Guid> vipListingIds = [];

        if (projectedListingIds.Length > 0)
        {
            List<Guid> ids =
                await dbContext.ListingPromotions
                    .AsNoTracking()
                    .Where(promotion =>
                        projectedListingIds.Contains(
                            promotion.ListingId) &&
                        promotion.Type ==
                            ListingPromotionType.Vip &&
                        promotion.StartsAtUtc <= now &&
                        promotion.EndsAtUtc > now)
                    .Select(promotion =>
                        promotion.ListingId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

            vipListingIds = ids.ToHashSet();
        }

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
                    favoriteCounts.GetValueOrDefault(
                        item.Id),
                    viewerFavoriteIds.Contains(
                        item.Id),
                    item.PublishedAtUtc,
                    item.ExpiresAtUtc)
                    {
                        IsVip =
                            vipListingIds.Contains(item.Id)
                    })
                .ToList();

        int totalPages =
            totalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    totalCount /
                    (double)query.PageSize);

        return new PagedResult<
            PublicListingSummaryResult>(
                items,
                query.Page,
                query.PageSize,
                totalCount,
                totalPages);
    }

    private static Guid[] GetAllowedCategoryIds(
        Guid? categoryId,
        List<Category> categories)
    {
        if (!categoryId.HasValue)
        {
            return [];
        }

        bool categoryExists =
            categories.Any(category =>
                category.Id == categoryId.Value);

        if (!categoryExists)
        {
            throw new DomainException(
                "Category was not found.");
        }

        Dictionary<Guid, List<Category>> childrenByParent =
    categories
        .Where(category =>
            category.ParentId.HasValue)
        .GroupBy(category =>
            category.ParentId!.Value)
        .ToDictionary(
            group => group.Key,
            group => group.ToList());

        List<Guid> result = [];
        Queue<Guid> pending = [];

        pending.Enqueue(categoryId.Value);

        while (pending.Count > 0)
        {
            Guid currentId = pending.Dequeue();

            result.Add(currentId);

            if (!childrenByParent.TryGetValue(
                    currentId,
                    out List<Category>? children))
            {
                continue;
            }

            foreach (Category child in children)
            {
                pending.Enqueue(child.Id);
            }
        }

        return result.ToArray();
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



    private static string? NormalizeSearch(
        string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        string normalized = search.Trim();

        if (normalized.Length > MaximumSearchLength)
        {
            throw new DomainException(
                "Search text cannot exceed 100 characters.");
        }

        return normalized;
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

    private sealed record ListingProjection(
        Guid Id,
        Guid OwnerId,
        Guid CategoryId,
        string Title,
        decimal Price,
        string Currency,
        int RentalPeriodUnit,
        Guid? CoverImageId,
        long ViewCount,
        DateTimeOffset PublishedAtUtc,
        DateTimeOffset ExpiresAtUtc);

    public async Task<PublicListingDetailsResult?> GetByIdAsync(
        Guid listingId,
        PreferredLanguage language,
        Guid? viewerUserId,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = clock.UtcNow;

        Listing? listing =
            await dbContext.Listings
                .AsNoTracking()
                .AsSplitQuery()
                .Include(item => item.Images)
                .Include(item => item.FieldValues)
                    .ThenInclude(value => value.Selections)
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == listingId &&
                        item.Status == ListingStatus.Active &&
                        item.ExpiresAtUtc.HasValue &&
                        item.ExpiresAtUtc.Value > now,
                    cancellationToken);

        if (listing is null)
        {
            return null;
        }

        long viewCount;

        if (viewerUserId.HasValue &&
            viewerUserId.Value == listing.OwnerId)
        {
            viewCount = listing.ViewCount;
        }
        else
        {
            viewCount =
                await viewRecorder.RecordAsync(
                    listing.Id,
                    viewerUserId,
                    cancellationToken);
        }

        int favoriteCount =
    await dbContext.Favorites
        .AsNoTracking()
        .CountAsync(
            favorite =>
                favorite.ListingId ==
                    listing.Id,
            cancellationToken);

        bool isFavorite =
            viewerUserId.HasValue &&
            await dbContext.Favorites
                .AsNoTracking()
                .AnyAsync(
                    favorite =>
                        favorite.UserId ==
                            viewerUserId.Value &&
                        favorite.ListingId ==
                            listing.Id,
                    cancellationToken);

        Category? category =
            await dbContext.Categories
                .AsNoTracking()
                .Include(item => item.Translations)
                .SingleOrDefaultAsync(
                    item => item.Id == listing.CategoryId,
                    cancellationToken);

        UserProfile? owner =
            await dbContext.UserProfiles
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.Id == listing.OwnerId,
                    cancellationToken);

        string? phoneNumber =
            await dbContext.Users
                .AsNoTracking()
                .Where(user => user.Id == listing.OwnerId)
                .Select(user => user.PhoneNumber)
                .SingleOrDefaultAsync(cancellationToken);

        Guid[] fieldIds =
            listing.FieldValues
                .Select(value => value.CategoryFieldId)
                .Distinct()
                .ToArray();

        List<CategoryField> fieldDefinitions =
            await dbContext.CategoryFields
                .AsNoTracking()
                .AsSplitQuery()
                .Include(field => field.Translations)
                .Include(field => field.Options)
                    .ThenInclude(option => option.Translations)
                .Where(field => fieldIds.Contains(field.Id))
                .ToListAsync(cancellationToken);

        Dictionary<Guid, CategoryField> fieldById =
            fieldDefinitions.ToDictionary(field => field.Id);

        string categoryName =
            category?.Translations
                .FirstOrDefault(item => item.Language == language)
                ?.Name
            ?? category?.Translations
                .FirstOrDefault(item =>
                    item.Language == PreferredLanguage.Azerbaijani)
                ?.Name
            ?? category?.Translations.FirstOrDefault()?.Name
            ?? "Unknown";

        List<ListingImageItemResult> images =
            listing.Images
                .OrderBy(image => image.DisplayOrder)
                .Select(image =>
                    new ListingImageItemResult(
                        image.Id,
                        image.DisplayOrder,
                        image.IsCover))
                .ToList();

        List<ListingFieldValueDetailsResult> fields =
            listing.FieldValues
                .Select(value =>
                    MapFieldValue(
                        value,
                        fieldById,
                        language))
                .OrderBy(field => field.Key)
                .ToList();

        bool isVip =
            await dbContext.ListingPromotions
                .AsNoTracking()
                .AnyAsync(
                    promotion =>
                        promotion.ListingId == listing.Id &&
                        promotion.Type ==
                            ListingPromotionType.Vip &&
                        promotion.StartsAtUtc <= now &&
                        promotion.EndsAtUtc > now,
                    cancellationToken);

        return new PublicListingDetailsResult(
            listing.Id,
            listing.CategoryId,
            categoryName,
            listing.Title,
            listing.Description,
            listing.Price,
            listing.Currency,
            (int)listing.RentalPeriodUnit,
            viewCount,
            favoriteCount,
            isFavorite,
            listing.PublishedAtUtc.GetValueOrDefault(),
            listing.ExpiresAtUtc.GetValueOrDefault(),
            new PublicListingOwnerResult(
                listing.OwnerId,
                owner?.FullName ?? "RentoX user",
                phoneNumber ?? string.Empty),
            images,
            fields)
        {
            IsVip = isVip
        };
    }

    private static ListingFieldValueDetailsResult MapFieldValue(
    ListingFieldValue value,
    Dictionary<Guid, CategoryField> fieldById,
    PreferredLanguage language)
    {
        if (!fieldById.TryGetValue(
                value.CategoryFieldId,
                out CategoryField? definition))
        {
            return new ListingFieldValueDetailsResult(
                value.CategoryFieldId,
                "unknown",
                "Unknown",
                0,
                value.TextValue,
                value.NumericValue,
                value.FlagValue,
                value.CalendarValue,
                value.CustomValue,
                []);
        }

        Dictionary<Guid, CategoryFieldOption> optionById =
            definition.Options.ToDictionary(
                option => option.Id);

        List<ListingFieldSelectionValueResult> selections =
            value.Selections
                .Where(selection =>
                    optionById.ContainsKey(
                        selection.CategoryFieldOptionId))
                .Select(selection =>
                {
                    CategoryFieldOption option =
                        optionById[
                            selection.CategoryFieldOptionId];

                    return new ListingFieldSelectionValueResult(
                        option.Id,
                        option.Value,
                        GetOptionLabel(
                            option,
                            language));
                })
                .ToList();

        return new ListingFieldValueDetailsResult(
            definition.Id,
            definition.Key,
            GetFieldLabel(
                definition,
                language),
            (int)definition.Type,
            value.TextValue,
            value.NumericValue,
            value.FlagValue,
            value.CalendarValue,
            value.CustomValue,
            selections);
    }

    private static string GetFieldLabel(
        CategoryField field,
        PreferredLanguage language)
    {
        CategoryFieldTranslation? translation =
            field.Translations.FirstOrDefault(item =>
                item.Language == language)
            ?? field.Translations.FirstOrDefault(item =>
                item.Language ==
                PreferredLanguage.Azerbaijani)
            ?? field.Translations.FirstOrDefault();

        return translation?.Label ?? field.Key;
    }

    private static string GetOptionLabel(
        CategoryFieldOption option,
        PreferredLanguage language)
    {
        CategoryFieldOptionTranslation? translation =
            option.Translations.FirstOrDefault(item =>
                item.Language == language)
            ?? option.Translations.FirstOrDefault(item =>
                item.Language ==
                PreferredLanguage.Azerbaijani)
            ?? option.Translations.FirstOrDefault();

        return translation?.Label ?? option.Value;
    }
}