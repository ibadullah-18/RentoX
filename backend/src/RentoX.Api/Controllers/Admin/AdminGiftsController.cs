using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Authorization;
using RentoX.Application.Gifts;

namespace RentoX.Api.Controllers.Admin;

/// <summary>
/// Free VIP and bump from RentoX. Only the owner (SuperAdmin) can give them: nothing is charged and nothing counts as
/// income, so it is a decision about the business, not a routine moderation step.
/// </summary>
[ApiController]
[Route("api/admin/gifts")]
[Authorize(Policy = PolicyNames.AdminAccess, Roles = "SuperAdmin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminGiftsController(IGiftService giftService, ICurrentUserContext currentUserContext) : ControllerBase
{
    public sealed record ListingGiftRequest(Guid ListingId, int Type, int? Days);

    public sealed record StoreGiftRequest(Guid StoreId, int? Days);

    public sealed record FreeServicesRequest(Guid UserId, int Services);

    [HttpPut("free-services")]
    public async Task<IActionResult> SetFreeServicesAsync(FreeServicesRequest request, CancellationToken cancellationToken)
    {
        await giftService.SetFreeServicesAsync(ActorId(), request.UserId, request.Services, cancellationToken);
        return NoContent();
    }

    [HttpPost("listing-promotions")]
    public async Task<ActionResult<GiftResult>> GiftListingAsync(ListingGiftRequest request, CancellationToken cancellationToken) =>
        Ok(await giftService.GiftListingPromotionAsync(ActorId(), request.ListingId, request.Type, request.Days, cancellationToken));

    [HttpPost("store-vip")]
    public async Task<ActionResult<GiftResult>> GiftStoreAsync(StoreGiftRequest request, CancellationToken cancellationToken) =>
        Ok(await giftService.GiftStoreVipAsync(ActorId(), request.StoreId, request.Days, cancellationToken));

    private Guid ActorId()
    {
        if (!currentUserContext.IsAuthenticated || !currentUserContext.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication is required.");
        }

        return currentUserContext.UserId.Value;
    }
}
