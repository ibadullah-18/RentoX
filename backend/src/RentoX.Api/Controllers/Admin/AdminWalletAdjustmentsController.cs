using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Admin;
using RentoX.Application.Authorization;

namespace RentoX.Api.Controllers.Admin;

/// <summary>Money added to or taken from a wallet by hand. Needs the finance section.</summary>
[ApiController]
[Route("api/admin/users/{userId:guid}/wallet-adjustments")]
[Authorize(Policy = PolicyNames.AdminAccess)]
[Authorize(Policy = AdminSections.FinancePolicy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminWalletAdjustmentsController(IWalletAdjustmentService adjustmentService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<WalletAdjustmentResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult<WalletAdjustmentResult>> AdjustAsync(
        Guid userId,
        WalletAdjustmentRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await adjustmentService.AdjustAsync(
            userId, request.RequestId, request.Credit, request.Type, request.Amount, request.Reason, cancellationToken));
    }
}

/// <param name="RequestId">One per submitted form; sending it twice changes the wallet once.</param>
/// <param name="Credit">True adds money, false takes it.</param>
/// <param name="Type">Bonus 5, Refund 4 or AdminAdjustment 6 (a debit is always 6).</param>
public sealed record WalletAdjustmentRequest(Guid RequestId, bool Credit, int Type, decimal Amount, string Reason);
