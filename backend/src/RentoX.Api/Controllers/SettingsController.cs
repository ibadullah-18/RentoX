using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Settings;

namespace RentoX.Api.Controllers;

/// <summary>The settings the apps need to show (how long a listing lives).</summary>
[ApiController]
[Route("api/settings")]
public sealed class SettingsController(ISystemSettingsService settingsService) : ControllerBase
{
    [HttpGet]
    [ResponseCache(Duration = 30)]
    [ProducesResponseType<SystemSettingsValues>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemSettingsValues>> GetAsync(CancellationToken cancellationToken)
    {
        return Ok(await settingsService.GetCurrentAsync(cancellationToken));
    }
}
