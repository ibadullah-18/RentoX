using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Listings.Promotions;
using RentoX.Contracts.Listings.Promotions;

namespace RentoX.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/listings/{listingId:guid}/promotions")]
public sealed class ListingPromotionsController(
    IListingPromotionService promotionService)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(
        typeof(PurchaseListingPromotionResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    public async Task<
        ActionResult<PurchaseListingPromotionResponse>>
        PurchaseAsync(
            Guid listingId,
            PurchaseListingPromotionRequest request,
            CancellationToken cancellationToken)
    {
        string? userIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(
                userIdValue,
                out Guid ownerId))
        {
            return Unauthorized();
        }

        PurchaseListingPromotionResult result =
            await promotionService.PurchaseAsync(
                new PurchaseListingPromotionCommand(
                    ownerId,
                    listingId,
                    request.Type,
                    request.IdempotencyKey),
                cancellationToken);

        return Ok(
            new PurchaseListingPromotionResponse(
                result.PromotionId,
                result.ListingId,
                result.Type,
                result.ChargedAmount,
                result.Currency,
                result.RemainingBalance,
                result.WalletTransactionId,
                result.StartsAtUtc,
                result.EndsAtUtc,
                result.WasAlreadyProcessed));
    }
}
