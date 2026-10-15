using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Authorization;
using RentoX.Application.Listings.Billing;

namespace RentoX.Api.Controllers.Admin;

/// <summary>Changing the prices of the paid services. Needs the finance section.</summary>
[ApiController]
[Route("api/admin/tariffs")]
[Authorize(Policy = PolicyNames.AdminAccess)]
[Authorize(Policy = AdminSections.FinancePolicy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminTariffsController(ITariffService tariffService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<TariffValues>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TariffValues>> GetAsync(CancellationToken cancellationToken)
    {
        return Ok(await tariffService.GetCurrentAsync(cancellationToken));
    }

    [HttpGet("history")]
    [ProducesResponseType<IReadOnlyList<TariffHistoryItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TariffHistoryItem>>> HistoryAsync(
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await tariffService.GetHistoryAsync(take, cancellationToken));
    }

    [HttpPut]
    [ProducesResponseType<TariffValues>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TariffValues>> SetAsync(
        TariffValues request,
        CancellationToken cancellationToken)
    {
        return Ok(await tariffService.SetAsync(request, cancellationToken));
    }
}
