using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Authorization;
using RentoX.Application.Stores;

namespace RentoX.Api.Controllers.Admin;

/// <summary>The VIP store product: its name, price and length, the offer text, and sending the offer. Needs the finance section.</summary>
[ApiController]
[Route("api/admin/store-vip")]
[Authorize(Policy = PolicyNames.AdminAccess)]
[Authorize(Policy = AdminSections.FinancePolicy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminStoreVipController(IStoreVipService vipService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<StoreVipPlanValues>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StoreVipPlanValues>> GetAsync(CancellationToken cancellationToken)
    {
        return Ok(await vipService.GetPlanAsync(cancellationToken));
    }

    [HttpPut]
    [ProducesResponseType<StoreVipPlanValues>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StoreVipPlanValues>> SetAsync(
        StoreVipPlanValues request, CancellationToken cancellationToken)
    {
        return Ok(await vipService.SetPlanAsync(request, cancellationToken));
    }

    /// <summary>Notifies the owners of non-VIP stores (each at most once per interval).</summary>
    [HttpPost("offers")]
    [ProducesResponseType<StoreVipOffersResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StoreVipOffersResult>> SendOffersAsync(CancellationToken cancellationToken)
    {
        return Ok(await vipService.SendOffersAsync(cancellationToken));
    }
}
