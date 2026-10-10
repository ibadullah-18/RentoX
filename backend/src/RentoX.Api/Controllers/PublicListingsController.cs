using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Common;
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
    ICurrentUserContext currentUserContext)
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
            Sort = order
        };

        PagedResult<PublicListingSummaryResult> result =
        await queryService.SearchAsync(
            query,
            preferredLanguage.Value,
            viewerUserId,
            cancellationToken);

        PublicListingSummaryResponse[] items =
            result.Items.Select(ToSummary).ToArray();

        return Ok(
            new PagedResponse<
                PublicListingSummaryResponse>(
                    items,
                    result.Page,
                    result.PageSize,
                    result.TotalCount,
                    result.TotalPages));
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

        IReadOnlyList<PublicListingSummaryResult> similar =
            await queryService.GetSimilarAsync(
                listingId,
                preferredLanguage.Value,
                viewerUserId,
                limit,
                cancellationToken);

        return Ok(similar.Select(ToSummary).ToList());
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
            IsVip = item.IsVip
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
            IsVip = result.IsVip
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