using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RentoX.Api.Caching;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Common;
using RentoX.Application.Favorites;
using RentoX.Application.Listings;
using RentoX.Contracts.Common;
using RentoX.Contracts.Listings;
using RentoX.Domain.Users.Enums;

namespace RentoX.Api.Controllers;

[ApiController]
[Route("api/listings")]
public sealed class PublicListingsController(
    IPublicListingQueryService queryService,
    IListingSuggestionService suggestionService,
    ICurrentUserContext currentUserContext,
    AnonymousResponseCache responseCache,
    IFavoriteService favoriteService)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<
        PagedResponse<PublicListingSummaryResponse>>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<
        PagedResponse<PublicListingSummaryResponse>>>
        SearchAsync(
            [FromQuery] Guid? categoryId = null,
            [FromQuery] string? search = null,
            [FromQuery] string language = "az",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] Guid? optionId = null,
            [FromQuery] Guid[]? optionIds = null,
            [FromQuery] Guid? numericFieldId = null,
            [FromQuery] decimal? numericMin = null,
            [FromQuery] decimal? numericMax = null,
            [FromQuery] Guid? booleanFieldId = null,
            [FromQuery] bool? booleanValue = null,
            [FromQuery] Guid? dateFieldId = null,
            [FromQuery] DateOnly? dateFrom = null,
            [FromQuery] DateOnly? dateTo = null,
            [FromQuery] string? filters = null,
            [FromQuery] string? sellerType = null,
            [FromQuery] string? sort = null,
            [FromQuery] Guid? cityId = null,
            [FromQuery] Guid? districtId = null,
            [FromQuery] int? rentalPeriod = null,
            [FromQuery] string? feed = null,
            CancellationToken cancellationToken = default)
    {
        PreferredLanguage? preferredLanguage =
            ParseLanguage(language);

        if (!preferredLanguage.HasValue)
        {
            return BadRequest(
                "Language must be az, ru or en.");
        }

        Guid? viewerUserId =
            currentUserContext.IsAuthenticated
                ? currentUserContext.UserId
                : null;

        if ((minPrice.HasValue && minPrice.Value < 0) ||
            (maxPrice.HasValue && maxPrice.Value < 0))
        {
            return BadRequest(
                "Price cannot be negative.");
        }

        if (minPrice.HasValue &&
            maxPrice.HasValue &&
            minPrice.Value > maxPrice.Value)
        {
            return BadRequest(
                "Minimum price cannot exceed maximum price.");
        }
        PublicListingSellerType seller;
        PublicListingSort order;

        switch (sellerType?.Trim().ToLowerInvariant())
        {
            case null or "" or "all":
                seller = PublicListingSellerType.Any;
                break;
            case "store":
                seller = PublicListingSellerType.Store;
                break;
            case "individual":
                seller = PublicListingSellerType.Individual;
                break;
            default:
                return BadRequest(
                    "Seller type must be all, store or individual.");
        }

        switch (sort?.Trim().ToLowerInvariant())
        {
            case null or "" or "date":
                order = PublicListingSort.Default;
                break;
            case "price_asc":
                order = PublicListingSort.PriceAscending;
                break;
            case "price_desc":
                order = PublicListingSort.PriceDescending;
                break;
            default:
                return BadRequest(
                    "Sort must be date, price_asc or price_desc.");
        }

        IReadOnlyList<PublicListingFieldFilter>? fieldFilters = null;

        if (!string.IsNullOrWhiteSpace(filters))
        {
            try
            {
                fieldFilters =
                    JsonSerializer.Deserialize<
                        List<ListingFieldFilterRequest>>(
                        filters,
                        FilterJson)?
                    .Select(item => new PublicListingFieldFilter(
                        item.FieldId,
                        item.OptionIds,
                        item.Min,
                        item.Max,
                        item.Flag,
                        item.From,
                        item.To))
                    .ToList();
            }
            catch (JsonException)
            {
                return BadRequest(
                    "Filters must be a JSON array of field conditions.");
            }
        }

        PublicListingSearchQuery query = new(
            categoryId,
            search,
            page,
            pageSize,
            MinPrice: minPrice,
            MaxPrice: maxPrice);

        query = query with
        {
            OptionId = optionId,
            OptionIds = optionIds,
            NumericFieldId = numericFieldId,
            NumericMin = numericMin,
            NumericMax = numericMax,
            BooleanFieldId = booleanFieldId,
            BooleanValue = booleanValue,
            DateFieldId = dateFieldId,
            DateFrom = dateFrom,
            DateTo = dateTo,
            FieldFilters = fieldFilters,
            SellerType = seller,
            Sort = order,
            CityId = cityId,
            DistrictId = districtId,
            RentalPeriodUnit = rentalPeriod,
            WideFeed = string.Equals(feed, "wide", StringComparison.OrdinalIgnoreCase)
        };

        async Task<PagedResponse<PublicListingSummaryResponse>> LoadAsync(CancellationToken token)
        {
            // Always the signed-out answer: it is the same for everybody (the hearts are added below).
            PagedResult<PublicListingSummaryResult> result =
                await queryService.SearchAsync(
                    query,
                    preferredLanguage.Value,
                    null,
                    token);

            return new PagedResponse<PublicListingSummaryResponse>(
                result.Items.Select(ToSummary).ToArray(),
                result.Page,
                result.PageSize,
                result.TotalCount,
                result.TotalPages);
        }

        // The answer does not depend on who asks, so it is kept for a few seconds: a crowd opening the home page at once
        // costs the database one set of queries, not one per person.
        string cacheKey = Request.QueryString.Value ?? string.Empty;
        PagedResponse<PublicListingSummaryResponse> answer =
            cacheKey.Length <= 600
                ? await responseCache.GetOrCreateAsync(cacheKey, () => LoadAsync(CancellationToken.None))
                : await LoadAsync(cancellationToken);

        if (viewerUserId is null || answer.Items.Count == 0)
        {
            return Ok(answer);
        }

        // A signed-in visitor also sees which of these listings are in their favourites (one small query).
        IReadOnlySet<Guid> favorites = await favoriteService.GetFavoriteIdsAsync(
            viewerUserId.Value,
            answer.Items.Select(item => item.Id).ToArray(),
            cancellationToken);

        return Ok(answer with
        {
            Items = answer.Items
                .Select(item => favorites.Contains(item.Id) ? item with { IsFavorite = true } : item)
                .ToArray()
        });
    }

    [HttpGet("suggestions")]
    [ProducesResponseType<IReadOnlyList<ListingSuggestionResponse>>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<
        IReadOnlyList<ListingSuggestionResponse>>>
        SuggestAsync(
            [FromQuery] string? q = null,
            [FromQuery] string language = "az",
            CancellationToken cancellationToken = default)
    {
        PreferredLanguage? preferredLanguage = ParseLanguage(language);

        if (!preferredLanguage.HasValue)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid language.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        IReadOnlyList<ListingSuggestionResult> suggestions =
            await suggestionService.SuggestAsync(
                q,
                preferredLanguage.Value,
                cancellationToken);

        return Ok(suggestions
            .Select(item => new ListingSuggestionResponse(
                item.Kind,
                item.Text,
                item.CategoryId,
                item.CategoryPath))
            .ToList());
    }

    [HttpGet("{listingId:guid}/similar")]
    [ProducesResponseType<IReadOnlyList<PublicListingSummaryResponse>>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<
        IReadOnlyList<PublicListingSummaryResponse>>>
        SimilarAsync(
            Guid listingId,
            [FromQuery] string language = "az",
            [FromQuery] int limit = 12,
            CancellationToken cancellationToken = default)
    {
        PreferredLanguage? preferredLanguage = ParseLanguage(language);

        if (!preferredLanguage.HasValue)
        {
            return BadRequest("Language must be az, ru or en.");
        }

        if (limit is < 1 or > 24)
        {
            return BadRequest("Limit must be between 1 and 24.");
        }

        Guid? viewerUserId =
            currentUserContext.IsAuthenticated
                ? currentUserContext.UserId
                : null;

        // The same for everybody, kept for a few seconds (finding similar listings is the heaviest query of a listing
        // page); the hearts of a signed-in visitor are added after.
        IReadOnlyList<PublicListingSummaryResponse> similar =
            await responseCache.GetOrCreateAsync(
                $"similar:{listingId:N}:{preferredLanguage.Value}:{limit}",
                async () => (IReadOnlyList<PublicListingSummaryResponse>)
                    (await queryService.GetSimilarAsync(
                        listingId,
                        preferredLanguage.Value,
                        null,
                        limit,
                        CancellationToken.None))
                    .Select(ToSummary)
                    .ToList());

        if (viewerUserId is null || similar.Count == 0)
        {
            return Ok(similar);
        }

        IReadOnlySet<Guid> favorites = await favoriteService.GetFavoriteIdsAsync(
            viewerUserId.Value,
            similar.Select(item => item.Id).ToArray(),
            cancellationToken);

        return Ok(similar
            .Select(item => favorites.Contains(item.Id) ? item with { IsFavorite = true } : item)
            .ToList());
    }

    private static PublicListingSummaryResponse ToSummary(
        PublicListingSummaryResult item) =>
        new(
            item.Id,
            item.OwnerId,
            item.CategoryId,
            item.CategoryName,
            item.Title,
            item.Price,
            item.Currency,
            item.RentalPeriodUnit,
            item.CoverImageId.HasValue
                ? $"/api/listing-images/{item.CoverImageId}"
                : null,
            item.ViewCount,
            item.FavoriteCount,
            item.IsFavorite,
            item.PublishedAtUtc,
            item.ExpiresAtUtc)
        {
            IsVip = item.IsVip,
            Location = item.Location
        };

    [HttpGet("{listingId:guid}")]
    [ProducesResponseType(
        typeof(PublicListingDetailsResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<
        ActionResult<PublicListingDetailsResponse>>
        GetByIdAsync(
            Guid listingId,
            [FromQuery] string language = "az",
            CancellationToken cancellationToken = default)
    {
        PreferredLanguage? preferredLanguage =
            ParseLanguage(language);

        if (!preferredLanguage.HasValue)
        {
            return BadRequest(
                "Language must be az, ru or en.");
        }

        Guid? viewerUserId =
            currentUserContext.IsAuthenticated
                ? currentUserContext.UserId
                : null;

        PublicListingDetailsResult? result =
            await queryService.GetByIdAsync(
                listingId,
                preferredLanguage.Value,
                viewerUserId,
                cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        List<ListingImageItemResponse> images =
            result.Images
                .Select(image =>
                    new ListingImageItemResponse(
                        image.Id,
                        $"/api/listing-images/{image.Id}",
                        image.DisplayOrder,
                        image.IsCover))
                .ToList();

        List<ListingFieldValueDetailsResponse> fields =
            result.Fields
                .Select(field =>
                    new ListingFieldValueDetailsResponse(
                        field.FieldId,
                        field.Key,
                        field.Label,
                        field.Type,
                        field.TextValue,
                        field.NumericValue,
                        field.FlagValue,
                        field.CalendarValue,
                        field.CustomValue,
                        field.Selections
                            .Select(selection =>
                                new ListingFieldSelectionValueResponse(
                                    selection.OptionId,
                                    selection.Value,
                                    selection.Label))
                            .ToList()))
                .ToList();

        PublicListingDetailsResponse response = new(
            result.Id,
            result.CategoryId,
            result.CategoryName,
            result.Title,
            result.Description,
            result.Price,
            result.Currency,
            result.RentalPeriodUnit,
            result.ViewCount,
            result.FavoriteCount,
            result.IsFavorite,
            result.PublishedAtUtc,
            result.ExpiresAtUtc,
            new PublicListingOwnerResponse(
                result.Owner.Id,
                result.Owner.FullName,
                result.Owner.PhoneNumber)
            {
                Store = result.Owner.Store is { } ownerStore
                    ? new PublicListingOwnerStoreResponse(
                        ownerStore.Id,
                        ownerStore.Name,
                        ownerStore.Slug,
                        ownerStore.HasLogoImage
                            ? $"/api/stores/{ownerStore.Id}/logo?v={ownerStore.ImageVersion}"
                            : null)
                    : null
            },
            images,
            fields)
        {
            IsVip = result.IsVip,
            CityId = result.CityId,
            CityName = result.CityName,
            DistrictId = result.DistrictId,
            DistrictName = result.DistrictName
        };

        return Ok(response);
    }

    private static readonly JsonSerializerOptions FilterJson =
        new(JsonSerializerDefaults.Web);

    private static PreferredLanguage? ParseLanguage(
        string language)
    {
        return language.Trim().ToLowerInvariant() switch
        {
            "az" => PreferredLanguage.Azerbaijani,
            "ru" => PreferredLanguage.Russian,
            "en" => PreferredLanguage.English,
            _ => null
        };
    }
}