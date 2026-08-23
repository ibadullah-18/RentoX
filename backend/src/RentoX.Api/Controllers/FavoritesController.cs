using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Common;
using RentoX.Application.Favorites;
using RentoX.Application.Listings;
using RentoX.Contracts.Common;
using RentoX.Contracts.Listings;
using RentoX.Domain.Users.Enums;

namespace RentoX.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/favorites")]
public sealed class FavoritesController(
    IFavoriteService favoriteService,
    IFavoriteQueryService favoriteQueryService,
    ICurrentUserContext currentUserContext)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<
        PagedResponse<PublicListingSummaryResponse>>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<
        PagedResponse<PublicListingSummaryResponse>>>
        GetMineAsync(
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

        Guid userId = GetRequiredUserId();

        PagedResult<PublicListingSummaryResult> result =
            await favoriteQueryService.GetMineAsync(
                userId,
                preferredLanguage.Value,
                page,
                pageSize,
                cancellationToken);

        PublicListingSummaryResponse[] items =
            result.Items
                .Select(MapSummary)
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

    [HttpPost("{listingId:guid}")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddAsync(
        Guid listingId,
        CancellationToken cancellationToken)
    {
        Guid userId = GetRequiredUserId();

        await favoriteService.AddAsync(
            userId,
            listingId,
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{listingId:guid}")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveAsync(
        Guid listingId,
        CancellationToken cancellationToken)
    {
        Guid userId = GetRequiredUserId();

        await favoriteService.RemoveAsync(
            userId,
            listingId,
            cancellationToken);

        return NoContent();
    }

    [HttpGet("{listingId:guid}/status")]
    [ProducesResponseType<bool>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<bool>>
        GetStatusAsync(
            Guid listingId,
            CancellationToken cancellationToken)
    {
        Guid userId = GetRequiredUserId();

        bool isFavorite =
            await favoriteService.IsFavoriteAsync(
                userId,
                listingId,
                cancellationToken);

        return Ok(isFavorite);
    }

    private static PublicListingSummaryResponse MapSummary(
        PublicListingSummaryResult item)
    {
        return new PublicListingSummaryResponse(
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
            item.ExpiresAtUtc);
    }

    private Guid GetRequiredUserId()
    {
        if (!currentUserContext.IsAuthenticated ||
            !currentUserContext.UserId.HasValue)
        {
            throw new UnauthorizedAccessException(
                "Authentication is required.");
        }

        return currentUserContext.UserId.Value;
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