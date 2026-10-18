using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Stores;
using RentoX.Contracts.Stores;

namespace RentoX.Api.Controllers;

/// <summary>The VIP store place: the stores shown on the home page, and the owner's offer and purchase.</summary>
[ApiController]
[Route("api/stores")]
public sealed class StoreVipController(
    IStoreVipService vipService,
    ICurrentUserContext currentUserContext)
    : ControllerBase
{
    [HttpGet("vip")]
    [ProducesResponseType<IReadOnlyList<PublicStoreSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PublicStoreSummaryResponse>>> ListAsync(
        [FromQuery] int take = 10,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PublicStoreSummaryResult> stores = await vipService.GetVipStoresAsync(take, cancellationToken);

        return Ok(stores
            .Select(item => new PublicStoreSummaryResponse(
                item.StoreId,
                item.Name,
                item.Slug,
                item.Description,
                item.HasLogoImage ? $"/api/stores/{item.StoreId}/logo?v={item.ImageVersion}" : null,
                item.ActiveListingCount,
                item.FollowerCount))
            .ToArray());
    }

    [Authorize]
    [HttpGet("mine/vip")]
    [ProducesResponseType<StoreVipStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StoreVipStatusResponse>> StatusAsync(CancellationToken cancellationToken)
    {
        StoreVipStatusResult? status = await vipService.GetStatusAsync(RequiredUserId(), cancellationToken);
        if (status is null)
        {
            return NotFound();
        }

        return Ok(new StoreVipStatusResponse(
            status.Plan.Name,
            status.Plan.Price,
            status.Plan.DurationDays,
            status.Plan.IsEnabled ? status.Plan.OfferMessage : string.Empty,
            status.Plan.IsEnabled,
            status.IsVip,
            status.VipUntilUtc,
            status.Currency,
            status.IsGift,
            status.FreeServices));
    }

    [Authorize]
    [HttpPost("mine/vip")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("store-event")]
    [ProducesResponseType<StoreVipPurchaseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StoreVipPurchaseResponse>> PurchaseAsync(
        StoreVipPurchaseRequest request,
        CancellationToken cancellationToken)
    {
        StoreVipPurchaseResult result = await vipService.PurchaseAsync(
            RequiredUserId(), request.IdempotencyKey, cancellationToken);

        return Ok(new StoreVipPurchaseResponse(
            result.StartsAtUtc, result.EndsAtUtc, result.ChargedAmount, result.RemainingBalance, result.IsGift));
    }

    private Guid RequiredUserId()
    {
        if (!currentUserContext.IsAuthenticated || !currentUserContext.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication is required.");
        }

        return currentUserContext.UserId.Value;
    }
}
