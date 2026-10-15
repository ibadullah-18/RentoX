using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Listings.Billing;

namespace RentoX.Api.Controllers;

/// <summary>The prices of the paid services, for the apps and the website to show.</summary>
[ApiController]
[Route("api/tariffs")]
public sealed class TariffsController(ITariffService tariffService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType<TariffValues>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TariffValues>> GetAsync(CancellationToken cancellationToken)
    {
        return Ok(await tariffService.GetCurrentAsync(cancellationToken));
    }
}
