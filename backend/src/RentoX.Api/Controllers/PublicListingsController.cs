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

        PublicListingSearchQuery query = new(
            categoryId,
            search,
            page,
            pageSize);

        PagedResult<PublicListingSummaryResult> result =
        await queryService.SearchAsync(
            query,
            preferredLanguage.Value,
            viewerUserId,
            cancellationToken);

        PublicListingSummaryResponse[] items =
            result.Items
                .Select(item =>
                    new PublicListingSummaryResponse(
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
                        item.ExpiresAtUtc))
                .ToArray();

        return Ok(
            new PagedResponse<
                PublicListingSummaryResponse>(
                    items,
                    result.Page,
                    result.PageSize,
                    result.TotalCount,
                    result.TotalPages));
    }

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
                result.Owner.PhoneNumber),
            images,
            fields);

        return Ok(response);
    }

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