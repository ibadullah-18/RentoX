using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Common;
using RentoX.Application.Stores;
using RentoX.Contracts.Common;
using RentoX.Contracts.Stores;

namespace RentoX.Api.Controllers;

[ApiController]
[Route("api/stores")]
public sealed class StoreFollowersController(
    IStoreFollowService followService,
    ICurrentUserContext currentUserContext)
    : ControllerBase
{
    [HttpPost("{storeId:guid}/follow")]
    [Authorize]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> FollowAsync(
        Guid storeId,
        CancellationToken cancellationToken)
    {
        await followService.FollowAsync(
            GetRequiredUserId(),
            storeId,
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{storeId:guid}/follow")]
    [Authorize]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UnfollowAsync(
        Guid storeId,
        CancellationToken cancellationToken)
    {
        await followService.UnfollowAsync(
            GetRequiredUserId(),
            storeId,
            cancellationToken);

        return NoContent();
    }

    [HttpGet("{storeId:guid}/follow-status")]
    [ProducesResponseType<StoreFollowStatusResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<
        ActionResult<StoreFollowStatusResponse>>
        GetStatusAsync(
            Guid storeId,
            CancellationToken cancellationToken)
    {
        Guid? userId =
            currentUserContext.IsAuthenticated
                ? currentUserContext.UserId
                : null;

        StoreFollowStatusResult? result =
            await followService.GetStatusAsync(
                userId,
                storeId,
                cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(new StoreFollowStatusResponse(
            result.StoreId,
            result.FollowerCount,
            result.IsFollowing));
    }

    [HttpGet("following")]
    [Authorize]
    [ProducesResponseType<
        PagedResponse<FollowedStoreResponse>>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<
        PagedResponse<FollowedStoreResponse>>>
        GetFollowingAsync(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
    {
        PagedResult<FollowedStoreResult> result =
            await followService.GetFollowingAsync(
                GetRequiredUserId(),
                page,
                pageSize,
                cancellationToken);

        FollowedStoreResponse[] items =
            result.Items
                .Select(item =>
                    new FollowedStoreResponse(
                        item.StoreId,
                        item.Name,
                        item.Slug,
                        item.HasLogoImage
                            ? $"/api/stores/{item.StoreId}/logo?v={item.ImageVersion}"
                            : null,
                        item.ActiveListingCount,
                        item.FollowerCount,
                        item.FollowedAtUtc))
                .ToArray();

        return Ok(new PagedResponse<
            FollowedStoreResponse>(
                items,
                result.Page,
                result.PageSize,
                result.TotalCount,
                result.TotalPages));
    }

    private Guid GetRequiredUserId()
    {
        if (!currentUserContext.UserId.HasValue)
        {
            throw new UnauthorizedAccessException(
                "Authenticated user was not found.");
        }

        return currentUserContext.UserId.Value;
    }
}