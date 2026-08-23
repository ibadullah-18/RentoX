using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Authorization;
using RentoX.Application.Common;
using RentoX.Application.Stores;
using RentoX.Contracts.Common;
using RentoX.Contracts.Stores;

namespace RentoX.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/stores")]
[Authorize(Policy = PolicyNames.AdminAccess)]
public sealed class AdminStoresController(
    IStoreModerationService moderationService)
    : ControllerBase
{
    [HttpGet("pending")]
    [ProducesResponseType<
        PagedResponse<StoreProfileResponse>>(
        StatusCodes.Status200OK)]
    public async Task<ActionResult<
        PagedResponse<StoreProfileResponse>>>
        GetPendingAsync(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
    {
        PagedResult<StoreProfileResult> result =
            await moderationService.GetPendingAsync(
                page,
                pageSize,
                cancellationToken);

        StoreProfileResponse[] items =
            result.Items
                .Select(MapProfile)
                .ToArray();

        return Ok(
            new PagedResponse<StoreProfileResponse>(
                items,
                result.Page,
                result.PageSize,
                result.TotalCount,
                result.TotalPages));
    }

    [HttpGet("{storeId:guid}")]
    [ProducesResponseType<StoreProfileResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StoreProfileResponse>>
        GetByIdAsync(
            Guid storeId,
            CancellationToken cancellationToken)
    {
        StoreProfileResult? result =
            await moderationService.GetByIdAsync(
                storeId,
                cancellationToken);

        return result is null
            ? NotFound()
            : Ok(MapProfile(result));
    }

    [HttpPost("{storeId:guid}/approve")]
    [ProducesResponseType<StoreStatusResponse>(
        StatusCodes.Status200OK)]
    public async Task<ActionResult<StoreStatusResponse>>
        ApproveAsync(
            Guid storeId,
            CancellationToken cancellationToken)
    {
        StoreStatusResult result =
            await moderationService.ApproveAsync(
                storeId,
                cancellationToken);

        return Ok(MapStatus(result));
    }

    [HttpPost("{storeId:guid}/reject")]
    [ProducesResponseType<StoreStatusResponse>(
        StatusCodes.Status200OK)]
    public async Task<ActionResult<StoreStatusResponse>>
        RejectAsync(
            Guid storeId,
            RejectStoreRequest request,
            CancellationToken cancellationToken)
    {
        StoreStatusResult result =
            await moderationService.RejectAsync(
                storeId,
                request.Reason,
                cancellationToken);

        return Ok(MapStatus(result));
    }

    private static StoreStatusResponse MapStatus(
        StoreStatusResult result)
    {
        return new StoreStatusResponse(
            result.StoreId,
            result.Status,
            result.RejectionReason,
            result.UpdatedAtUtc);
    }

    private static StoreProfileResponse MapProfile(
        StoreProfileResult result)
    {
        long version =
            (result.UpdatedAtUtc ??
             result.CreatedAtUtc)
                .ToUnixTimeMilliseconds();

        return new StoreProfileResponse(
            result.Id,
            result.OwnerId,
            result.Name,
            result.Slug,
            result.Description,
            result.PhoneNumber,
            result.Email,
            result.Address,
            result.LogoImageKey is null
                ? null
                : $"/api/stores/{result.Id}/logo?v={version}",
            result.CoverImageKey is null
                ? null
                : $"/api/stores/{result.Id}/cover?v={version}",
            result.InstagramUrl,
            result.TiktokUrl,
            result.FacebookUrl,
            result.WebsiteUrl,
            result.Status,
            result.RejectionReason,
            result.CreatedAtUtc,
            result.UpdatedAtUtc);
    }
}