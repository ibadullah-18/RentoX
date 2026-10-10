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
using RentoX.Domain.Stores;
using RentoX.Domain.Stores.Enums;
using RentoX.Domain.Users.Enums;
using RentoX.Application.Listings.Search;
using RentoX.Infrastructure.Listings.Search;
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

    public async Task<IReadOnlyList<PublicListingSummaryResult>>
        GetSimilarAsync(
            Guid listingId,
            PreferredLanguage language,
            Guid? viewerUserId,
            int limit,
            CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            limit,
            MaximumSimilarLimit);

        DateTimeOffset now = clock.UtcNow;

        var source =
            await dbContext.Listings
                .AsNoTracking()
                .Where(listing =>
                    listing.Id == listingId &&
                    listing.Status == ListingStatus.Active &&
                    listing.ExpiresAtUtc.HasValue &&
                    listing.ExpiresAtUtc > now)
                .Select(listing => new
                {
                    listing.CategoryId,
                    Values = listing.FieldValues
                        .Select(value => new
                        {
                            value.CategoryFieldId,
                            value.CustomValue,
                            OptionIds = value.Selections
                                .Select(selection =>
                                    selection.CategoryFieldOptionId)
                                .ToList()
                        })
                        .ToList()
                })
                .SingleOrDefaultAsync(cancellationToken);

        if (source is null)
        {
            return [];
        }

        Guid? parentId =
            await dbContext.Categories
                .AsNoTracking()
                .Where(category => category.Id == source.CategoryId)
                .Select(category => category.ParentId)
                .SingleOrDefaultAsync(cancellationToken);

        // "Same brand": the first filterable pick-from-a-list field this
        // listing has a value for (brand, make...).
        Guid[] valueFieldIds =
            source.Values
                .Select(value => value.CategoryFieldId)
                .ToArray();

        Guid? sameFieldId =
            await dbContext.CategoryFields
                .AsNoTracking()
                .Where(field =>
                    valueFieldIds.Contains(field.Id) &&
                    field.IsActive &&
                    field.IsFilterable &&
                    (field.Type == CategoryFieldType.SingleSelect ||
                     field.Type == CategoryFieldType.MultiSelect))
                .OrderBy(field => field.DisplayOrder)
                .Select(field => (Guid?)field.Id)
                .FirstOrDefaultAsync(cancellationToken);

        List<PublicListingSummaryResult> sameBrand = [];

        if (sameFieldId.HasValue)
        {
            var value = source.Values.First(item =>
                item.CategoryFieldId == sameFieldId.Value);

            sameBrand = await SimilarPoolAsync(
                source.CategoryId,
                new PublicListingFieldFilter(
                    sameFieldId.Value,
                    OptionIds: value.OptionIds.Count > 0
                        ? [.. value.OptionIds]
                        : null,
                    CustomValue: value.CustomValue),
                language,
                viewerUserId,
                cancellationToken);
        }

        List<PublicListingSummaryResult> sameCategory =
            await SimilarPoolAsync(
                source.CategoryId,
                null,
                language,
                viewerUserId,
                cancellationToken);

        List<PublicListingSummaryResult> nearby =
            parentId.HasValue
                ? await SimilarPoolAsync(
                    parentId.Value,
                    null,
                    language,
                    viewerUserId,
                    cancellationToken)
                : [];

        return MixSimilar(
            listingId,
            limit,
            sameBrand,
            sameCategory,
            nearby);
    }

    private async Task<List<PublicListingSummaryResult>> SimilarPoolAsync(
        Guid categoryId,
        PublicListingFieldFilter? filter,
        PreferredLanguage language,
        Guid? viewerUserId,
        CancellationToken cancellationToken)
    {
        PagedResult<PublicListingSummaryResult> page =
            await SearchAsync(
                new PublicListingSearchQuery(
                    categoryId,
                    null,
                    1,
                    SimilarPoolSize,
                    FieldFilters: filter is null ? null : [filter]),
                language,
                viewerUserId,
                cancellationToken);

        return [.. page.Items];
    }

    /// <summary>
    /// Takes about half from the same brand, fills up from the same
    /// category and then from the neighbouring categories, and keeps any one
    /// seller from taking over (at most two each) unless there are too few
    /// listings otherwise.
    /// </summary>
    internal static IReadOnlyList<PublicListingSummaryResult> MixSimilar(
        Guid sourceListingId,
        int limit,
        IReadOnlyList<PublicListingSummaryResult> sameBrand,
        IReadOnlyList<PublicListingSummaryResult> sameCategory,
        IReadOnlyList<PublicListingSummaryResult> nearby)
    {
        List<PublicListingSummaryResult> result = [];
        HashSet<Guid> seen = [sourceListingId];
        Dictionary<Guid, int> perOwner = [];

        void Add(
            IReadOnlyList<PublicListingSummaryResult> pool,
            int ownerCap,
            int until)
        {
            foreach (PublicListingSummaryResult item in pool)
            {
                if (result.Count >= until)
                {
                    return;
                }

                if (seen.Contains(item.Id) ||
                    perOwner.GetValueOrDefault(item.OwnerId) >= ownerCap)
                {
                    continue;
                }

                seen.Add(item.Id);
                perOwner[item.OwnerId] =
                    perOwner.GetValueOrDefault(item.OwnerId) + 1;
                result.Add(item);
            }
        }

        int brandShare = (limit + 1) / 2;

        Add(sameBrand, SimilarOwnerCap, brandShare);
        Add(sameCategory, SimilarOwnerCap, limit);
        Add(nearby, SimilarOwnerCap, limit);

        // Too few listings overall: relax the per-seller cap.
        Add(sameBrand, int.MaxValue, limit);
        Add(sameCategory, int.MaxValue, limit);
        Add(nearby, int.MaxValue, limit);

        return result;
    }

    private const int MaximumSimilarLimit = 24;
    private const int SimilarPoolSize = 40;
    private const int SimilarOwnerCap = 2;

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

        // Smart search: words are matched with typo tolerance, synonyms and
        // category names, and scored in the database. `null` means the
        // database has not been upgraded for it yet (plain text fallback).
        IReadOnlyList<ListingSearchScore>? searchScores = null;

        if (search is not null)
        {
            searchScores =
                await ListingSearchScorer.ScoreAsync(
                    dbContext,
                    ListingSearchTerms.Parse(search),
                    now,
                    cancellationToken);

            if (searchScores is null)
            {
                listingQuery =
                    ApplyPlainTextSearch(listingQuery, search);
            }
            else
            {
                Guid[] candidateIds =
                    searchScores
                        .Select(score => score.Id)
                        .ToArray();

                listingQuery =
                    listingQuery.Where(listing =>
                        candidateIds.Contains(listing.Id));
            }
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
                            (field.Type == CategoryFieldType.WholeNumber ||
                             field.Type == CategoryFieldType.FractionalNumber),
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

        if (query.BooleanFieldId.HasValue !=
            query.BooleanValue.HasValue)
        {
            throw new DomainException(
                "Boolean field id and value are required together.");
        }

        if (query.BooleanFieldId.HasValue)
        {
            Guid booleanFieldId =
                query.BooleanFieldId.Value;

            bool validBooleanField =
                await dbContext.CategoryFields
                    .AsNoTracking()
                    .AnyAsync(
                        field =>
                            field.Id == booleanFieldId &&
                            field.IsActive &&
                            field.IsFilterable &&
                            field.Type ==
                                CategoryFieldType.Boolean,
                        cancellationToken);

            if (!validBooleanField)
            {
                throw new DomainException(
                    "Boolean filter field was not found or is not filterable.");
            }

            bool requiredValue =
                query.BooleanValue!.Value;

            listingQuery =
                listingQuery.Where(listing =>
                    listing.FieldValues.Any(value =>
                        value.CategoryFieldId ==
                            booleanFieldId &&
                        value.FlagValue ==
                            requiredValue));
        }

        bool hasDateBound =
            query.DateFrom.HasValue ||
            query.DateTo.HasValue;

        if (query.DateFieldId.HasValue != hasDateBound)
        {
            throw new DomainException(
                "Date field id and at least one date bound are required together.");
        }

        if (query.DateFrom.HasValue &&
            query.DateTo.HasValue &&
            query.DateFrom.Value > query.DateTo.Value)
        {
            throw new DomainException(
                "Start date cannot exceed end date.");
        }

        if (query.DateFieldId.HasValue)
        {
            Guid dateFieldId = query.DateFieldId.Value;

            bool validDateField =
                await dbContext.CategoryFields
                    .AsNoTracking()
                    .AnyAsync(
                        field =>
                            field.Id == dateFieldId &&
                            field.IsActive &&
                            field.IsFilterable &&
                            field.Type == CategoryFieldType.Date,
                        cancellationToken);

            if (!validDateField)
            {
                throw new DomainException(
                    "Date filter field was not found or is not filterable.");
            }

            DateOnly? dateFrom = query.DateFrom;
            DateOnly? dateTo = query.DateTo;

            listingQuery =
                listingQuery.Where(listing =>
                    listing.FieldValues.Any(value =>
                        value.CategoryFieldId == dateFieldId &&
                        value.CalendarValue.HasValue &&
                        (!dateFrom.HasValue ||
                         value.CalendarValue >= dateFrom) &&
                        (!dateTo.HasValue ||
                         value.CalendarValue <= dateTo)));
        }

        if (query.SellerType != PublicListingSellerType.Any)
        {
            bool wantStore =
                query.SellerType == PublicListingSellerType.Store;

            listingQuery =
                listingQuery.Where(listing =>
                    dbContext.Set<StoreProfile>().Any(store =>
                        store.OwnerId == listing.OwnerId &&
                        store.Status == StoreStatus.Active) == wantStore);
        }

        if (query.FieldFilters is { Count: > 0 } fieldFilters)
        {
            listingQuery = await ApplyFieldFiltersAsync(
                listingQuery,
                fieldFilters,
                cancellationToken);
        }

        int totalCount;
        List<ListingProjection> projections;

        bool sortByPrice = query.Sort != PublicListingSort.Default;

        if (searchScores is { Count: > 0 } && !sortByPrice)
        {
            // Relevance first; a live VIP promotion adds a small boost and
            // newer listings win ties.
            Dictionary<Guid, double> scoreById =
                searchScores.ToDictionary(
                    score => score.Id,
                    score => score.Score);

            var candidates =
                await listingQuery
                    .Select(listing => new
                    {
                        listing.Id,
                        IsVip =
                            dbContext.ListingPromotions.Any(promotion =>
                                promotion.ListingId == listing.Id &&
                                promotion.Type == ListingPromotionType.Vip &&
                                promotion.StartsAtUtc <= now &&
                                promotion.EndsAtUtc > now),
                        Recency =
                            dbContext.ListingPromotions
                                .Where(promotion =>
                                    promotion.ListingId == listing.Id &&
                                    promotion.Type == ListingPromotionType.Bump &&
                                    promotion.StartsAtUtc >= listing.PublishedAtUtc)
                                .Max(promotion =>
                                    (DateTimeOffset?)promotion.StartsAtUtc)
                            ?? listing.PublishedAtUtc
                    })
                    .ToListAsync(cancellationToken);

            totalCount = candidates.Count;

            Guid[] pageIds =
                candidates
                    .OrderByDescending(candidate =>
                        scoreById[candidate.Id] +
                        (candidate.IsVip ? VipSearchBoost : 0))
                    .ThenByDescending(candidate =>
                        candidate.Recency)
                    .ThenByDescending(candidate =>
                        candidate.Id)
                    .Skip(
                        (query.Page - 1) *
                        query.PageSize)
                    .Take(query.PageSize)
                    .Select(candidate => candidate.Id)
                    .ToArray();

            List<ListingProjection> unordered =
                await listingQuery
                    .Where(listing =>
                        pageIds.Contains(listing.Id))
                    .Select(ProjectListing)
                    .ToListAsync(cancellationToken);

            Dictionary<Guid, ListingProjection> byId =
                unordered.ToDictionary(item => item.Id);

            projections =
                pageIds
                    .Where(byId.ContainsKey)
                    .Select(id => byId[id])
                    .ToList();
        }
        else if (sortByPrice)
        {
            // An explicit price order replaces relevance and VIP ranking.
            totalCount =
                await listingQuery.CountAsync(
                    cancellationToken);

            IOrderedQueryable<Listing> ordered =
                query.Sort == PublicListingSort.PriceAscending
                    ? listingQuery.OrderBy(listing => listing.Price)
                    : listingQuery.OrderByDescending(
                        listing => listing.Price);

            projections =
                await ordered
                    .ThenByDescending(listing =>
                        listing.PublishedAtUtc)
                    .ThenByDescending(listing =>
                        listing.Id)
                    .Skip(
                        (query.Page - 1) *
                        query.PageSize)
                    .Take(query.PageSize)
                    .Select(ProjectListing)
                    .ToListAsync(cancellationToken);
        }
        else
        {
            // Plain browsing: VIP, bumped and plain listings take turns, so
            // a lot of paid listings cannot hide the free ones.
            IQueryable<Listing> vipQuery =
                listingQuery.Where(listing =>
                    dbContext.ListingPromotions.Any(promotion =>
                        promotion.ListingId == listing.Id &&
                        promotion.Type == ListingPromotionType.Vip &&
                        promotion.StartsAtUtc <= now &&
                        promotion.EndsAtUtc > now));

            DateTimeOffset bumpSince = now - BumpBoostWindow;

            IQueryable<Listing> bumpedQuery =
                listingQuery.Where(listing =>
                    !dbContext.ListingPromotions.Any(promotion =>
                        promotion.ListingId == listing.Id &&
                        promotion.Type == ListingPromotionType.Vip &&
                        promotion.StartsAtUtc <= now &&
                        promotion.EndsAtUtc > now) &&
                    dbContext.ListingPromotions.Any(promotion =>
                        promotion.ListingId == listing.Id &&
                        promotion.Type == ListingPromotionType.Bump &&
                        promotion.StartsAtUtc >= listing.PublishedAtUtc &&
                        promotion.StartsAtUtc >= bumpSince));

            IQueryable<Listing> plainQuery =
                listingQuery.Where(listing =>
                    !dbContext.ListingPromotions.Any(promotion =>
                        promotion.ListingId == listing.Id &&
                        promotion.Type == ListingPromotionType.Vip &&
                        promotion.StartsAtUtc <= now &&
                        promotion.EndsAtUtc > now) &&
                    !dbContext.ListingPromotions.Any(promotion =>
                        promotion.ListingId == listing.Id &&
                        promotion.Type == ListingPromotionType.Bump &&
                        promotion.StartsAtUtc >= listing.PublishedAtUtc &&
                        promotion.StartsAtUtc >= bumpSince));

            int vipCount = await vipQuery.CountAsync(cancellationToken);
            int bumpedCount = await bumpedQuery.CountAsync(cancellationToken);
            int plainCount = await plainQuery.CountAsync(cancellationToken);

            totalCount = vipCount + bumpedCount + plainCount;

            ListingFeedInterleaver.Plan plan =
                ListingFeedInterleaver.Build(
                    vipCount,
                    bumpedCount,
                    plainCount,
                    (query.Page - 1) * query.PageSize,
                    query.PageSize);

            IQueryable<Listing>[] kinds =
                [vipQuery, bumpedQuery, plainQuery];

            List<ListingProjection>[] fetched =
                new List<ListingProjection>[3];

            for (int kind = 0; kind < 3; kind++)
            {
                fetched[kind] =
                    plan.TakePerKind[kind] == 0
                        ? []
                        : await kinds[kind]
                            .OrderByDescending(listing =>
                                dbContext.ListingPromotions
                                    .Where(promotion =>
                                        promotion.ListingId == listing.Id &&
                                        promotion.Type ==
                                            ListingPromotionType.Bump &&
                                        promotion.StartsAtUtc >=
                                            listing.PublishedAtUtc)
                                    .Max(promotion =>
                                        (DateTimeOffset?)promotion.StartsAtUtc)
                                ?? listing.PublishedAtUtc)
                            .ThenByDescending(listing => listing.Id)
                            .Skip(plan.SkipPerKind[kind])
                            .Take(plan.TakePerKind[kind])
                            .Select(ProjectListing)
                            .ToListAsync(cancellationToken);
            }

            int[] next = [0, 0, 0];
            projections = [];

            foreach (int kind in plan.Page)
            {
                if (next[kind] < fetched[kind].Count)
                {
                    projections.Add(fetched[kind][next[kind]++]);
                }
            }
        }

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

    private const int MaximumFieldFilters = 15;

    /// <summary>
    /// Several dynamic-field conditions at once: options of one field are
    /// alternatives (OR), different fields all have to match (AND).
    /// </summary>
    private async Task<IQueryable<Listing>> ApplyFieldFiltersAsync(
        IQueryable<Listing> listingQuery,
        IReadOnlyList<PublicListingFieldFilter> filters,
        CancellationToken cancellationToken)
    {
        if (filters.Count > MaximumFieldFilters)
        {
            throw new DomainException(
                "Too many field filters.");
        }

        Guid[] fieldIds =
            filters.Select(filter => filter.FieldId).ToArray();

        if (fieldIds.Distinct().Count() != fieldIds.Length)
        {
            throw new DomainException(
                "Each field can be filtered only once.");
        }

        Dictionary<Guid, CategoryFieldType> types =
            await dbContext.CategoryFields
                .AsNoTracking()
                .Where(field =>
                    fieldIds.Contains(field.Id) &&
                    field.IsActive &&
                    field.IsFilterable)
                .ToDictionaryAsync(
                    field => field.Id,
                    field => field.Type,
                    cancellationToken);

        foreach (PublicListingFieldFilter filter in filters)
        {
            if (!types.TryGetValue(filter.FieldId, out CategoryFieldType type))
            {
                throw new DomainException(
                    "A filter field was not found or is not filterable.");
            }

            Guid fieldId = filter.FieldId;

            switch (type)
            {
                case CategoryFieldType.SingleSelect:
                case CategoryFieldType.MultiSelect:
                {
                    Guid[] optionIds =
                        filter.OptionIds?.Distinct().ToArray() ?? [];

                    // Exact, case-insensitive match (ILIKE without wildcards).
                    string? customValue =
                        string.IsNullOrWhiteSpace(filter.CustomValue)
                            ? null
                            : filter.CustomValue.Trim()
                                .Replace("\\", "\\\\", StringComparison.Ordinal)
                                .Replace("%", "\\%", StringComparison.Ordinal)
                                .Replace("_", "\\_", StringComparison.Ordinal);

                    if (optionIds.Length == 0 && customValue is null)
                    {
                        throw new DomainException(
                            "Select filters need at least one option.");
                    }

                    int valid =
                        await dbContext.CategoryFieldOptions
                            .AsNoTracking()
                            .CountAsync(
                                option =>
                                    optionIds.Contains(option.Id) &&
                                    option.CategoryFieldId == fieldId &&
                                    option.IsActive,
                                cancellationToken);

                    if (valid != optionIds.Length)
                    {
                        throw new DomainException(
                            "A filter option was not found for its field.");
                    }

                    bool hasOptions = optionIds.Length > 0;

                    listingQuery =
                        listingQuery.Where(listing =>
                            listing.FieldValues.Any(value =>
                                value.CategoryFieldId == fieldId &&
                                ((hasOptions &&
                                  value.Selections.Any(selection =>
                                      optionIds.Contains(
                                          selection.CategoryFieldOptionId))) ||
                                 (customValue != null &&
                                  value.CustomValue != null &&
                                  EF.Functions.ILike(
                                      value.CustomValue,
                                      customValue)))));
                    break;
                }

                case CategoryFieldType.WholeNumber:
                case CategoryFieldType.FractionalNumber:
                {
                    decimal? minimum = filter.Min;
                    decimal? maximum = filter.Max;

                    if (!minimum.HasValue && !maximum.HasValue)
                    {
                        throw new DomainException(
                            "Number filters need a minimum or a maximum.");
                    }

                    if (minimum > maximum)
                    {
                        throw new DomainException(
                            "Numeric minimum cannot exceed maximum.");
                    }

                    listingQuery =
                        listingQuery.Where(listing =>
                            listing.FieldValues.Any(value =>
                                value.CategoryFieldId == fieldId &&
                                value.NumericValue.HasValue &&
                                (!minimum.HasValue ||
                                 value.NumericValue >= minimum) &&
                                (!maximum.HasValue ||
                                 value.NumericValue <= maximum)));
                    break;
                }

                case CategoryFieldType.Boolean:
                {
                    if (!filter.Flag.HasValue)
                    {
                        throw new DomainException(
                            "Yes/no filters need a value.");
                    }

                    bool flag = filter.Flag.Value;

                    listingQuery =
                        listingQuery.Where(listing =>
                            listing.FieldValues.Any(value =>
                                value.CategoryFieldId == fieldId &&
                                value.FlagValue == flag));
                    break;
                }

                case CategoryFieldType.Date:
                {
                    DateOnly? from = filter.From;
                    DateOnly? to = filter.To;

                    if (!from.HasValue && !to.HasValue)
                    {
                        throw new DomainException(
                            "Date filters need a start or an end date.");
                    }

                    if (from > to)
                    {
                        throw new DomainException(
                            "Start date cannot exceed end date.");
                    }

                    listingQuery =
                        listingQuery.Where(listing =>
                            listing.FieldValues.Any(value =>
                                value.CategoryFieldId == fieldId &&
                                value.CalendarValue.HasValue &&
                                (!from.HasValue ||
                                 value.CalendarValue >= from) &&
                                (!to.HasValue ||
                                 value.CalendarValue <= to)));
                    break;
                }

                default:
                    throw new DomainException(
                        "This field type cannot be filtered.");
            }
        }

        return listingQuery;
    }

    /// <summary>How much a live VIP promotion adds to a search score.</summary>
    private const double VipSearchBoost = 2.0;

    /// <summary>How long a bump keeps a listing in the "bumped" group.</summary>
    private static readonly TimeSpan BumpBoostWindow = TimeSpan.FromDays(7);

    private static readonly System.Linq.Expressions.Expression<
        Func<Listing, ListingProjection>> ProjectListing =
        listing =>
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
                    listing.ExpiresAtUtc!.Value);

    /// <summary>The previous search: plain "contains" matching.</summary>
    private IQueryable<Listing> ApplyPlainTextSearch(
        IQueryable<Listing> listingQuery,
        string search)
    {
        string searchPattern = $"%{search}%";

        listingQuery =
            listingQuery.Where(listing =>
                EF.Functions.ILike(
                    listing.Title,
                    searchPattern) ||
                EF.Functions.ILike(
                    listing.Description,
                    searchPattern) ||
                listing.FieldValues.Any(value =>
                    dbContext.CategoryFields.Any(field =>
                        field.Id == value.CategoryFieldId &&
                        field.IsActive &&
                        field.IsSearchable) &&
                    (
                        (value.TextValue != null &&
                         EF.Functions.ILike(
                             value.TextValue!,
                             searchPattern)) ||
                        (value.CustomValue != null &&
                         EF.Functions.ILike(
                             value.CustomValue!,
                             searchPattern)) ||
                        value.Selections.Any(selection =>
                            dbContext.CategoryFieldOptions.Any(
                                option =>
                                    option.Id ==
                                        selection.CategoryFieldOptionId &&
                                    option.IsActive &&
                                    (
                                        EF.Functions.ILike(
                                            option.Value,
                                            searchPattern) ||
                                        option.Translations.Any(
                                            translation =>
                                                EF.Functions.ILike(
                                                    translation.Label,
                                                    searchPattern))
                                    )))
                    )));

        return listingQuery;
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

        var ownerStore =
            await dbContext.Set<StoreProfile>()
                .AsNoTracking()
                .Where(store =>
                    store.OwnerId == listing.OwnerId &&
                    store.Status == StoreStatus.Active)
                .Select(store => new
                {
                    store.Id,
                    store.Name,
                    store.Slug,
                    store.LogoImageKey,
                    store.CreatedAtUtc,
                    store.UpdatedAtUtc
                })
                .FirstOrDefaultAsync(cancellationToken);

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
                phoneNumber ?? string.Empty)
            {
                Store = ownerStore is null
                    ? null
                    : new PublicListingOwnerStoreResult(
                        ownerStore.Id,
                        ownerStore.Name,
                        ownerStore.Slug,
                        !string.IsNullOrWhiteSpace(
                            ownerStore.LogoImageKey),
                        (ownerStore.UpdatedAtUtc ??
                            ownerStore.CreatedAtUtc)
                            .ToUnixTimeMilliseconds())
            },
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