using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Common;
using RentoX.Application.Listings;
using RentoX.Application.Stores;
using RentoX.Contracts.Common;
using RentoX.Contracts.Listings;
using RentoX.Contracts.Stores;
using RentoX.Domain.Users.Enums;

namespace RentoX.Api.Controllers;

[ApiController]
[Route("api/stores")]
public sealed class PublicStoresController(
    IPublicStoreQueryService queryService,
    ICurrentUserContext currentUserContext)
    : ControllerBase
{
    [HttpGet("{slug}")]
    [ProducesResponseType<PublicStoreDetailsResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<
        ActionResult<PublicStoreDetailsResponse>>
        GetBySlugAsync(
            string slug,
            CancellationToken cancellationToken)
    {
        PublicStoreDetailsResult? result =
            await queryService.GetBySlugAsync(
                slug,
                cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        string? logoImageUrl =
            result.HasLogoImage
                ? $"/api/stores/{result.Id}/logo?v={result.ImageVersion}"
                : null;

        string? coverImageUrl =
            result.HasCoverImage
                ? $"/api/stores/{result.Id}/cover?v={result.ImageVersion}"
                : null;

        return Ok(new PublicStoreDetailsResponse(
            result.Id,
            result.Name,
            result.Slug,
            result.Description,
            result.PhoneNumber,
            result.Email,
            result.Address,
            logoImageUrl,
            coverImageUrl,
            result.InstagramUrl,
            result.TiktokUrl,
            result.FacebookUrl,
            result.WebsiteUrl,
            result.ActiveListingCount,
            result.TotalViewCount,
            result.TotalFavoriteCount,
            result.CreatedAtUtc));
    }

    [HttpGet("{slug}/listings")]
    [ProducesResponseType<
        PagedResponse<PublicListingSummaryResponse>>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<
        PagedResponse<PublicListingSummaryResponse>>>
        GetListingsAsync(
            string slug,
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

        PagedResult<PublicListingSummaryResult>? result =
            await queryService.GetListingsAsync(
                slug,
                preferredLanguage.Value,
                viewerUserId,
                page,
                pageSize,
                cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        PublicListingSummaryResponse[] items =
            result.Items
                .Select(MapListing)
                .ToArray();

        return Ok(new PagedResponse<
            PublicListingSummaryResponse>(
                items,
                result.Page,
                result.PageSize,
                result.TotalCount,
                result.TotalPages));
    }

    private static PublicListingSummaryResponse MapListing(
        PublicListingSummaryResult result)
    {
        return new PublicListingSummaryResponse(
            result.Id,
            result.OwnerId,
            result.CategoryId,
            result.CategoryName,
            result.Title,
            result.Price,
            result.Currency,
            result.RentalPeriodUnit,
            result.CoverImageId.HasValue
                ? $"/api/listing-images/{result.CoverImageId}"
                : null,
            result.ViewCount,
            result.FavoriteCount,
            result.IsFavorite,
            result.PublishedAtUtc,
            result.ExpiresAtUtc);
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