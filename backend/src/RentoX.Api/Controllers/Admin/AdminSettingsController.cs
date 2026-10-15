using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Authentication;
using RentoX.Application.Authorization;
using RentoX.Application.Settings;

namespace RentoX.Api.Controllers.Admin;

/// <summary>The adjustable settings of the site. Needs the listings section (they decide how listings live).</summary>
[ApiController]
[Route("api/admin/settings")]
[Authorize(Policy = PolicyNames.AdminAccess)]
[Authorize(Policy = AdminSections.ListingsPolicy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminSettingsController(ISystemSettingsService settingsService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<SystemSettingsValues>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemSettingsValues>> GetAsync(CancellationToken cancellationToken)
    {
        return Ok(await settingsService.GetCurrentAsync(cancellationToken));
    }

    [HttpGet("history")]
    [ProducesResponseType<IReadOnlyList<SystemSettingsHistoryItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SystemSettingsHistoryItem>>> HistoryAsync(
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await settingsService.GetHistoryAsync(take, cancellationToken));
    }

    [HttpPut]
    [ProducesResponseType<SystemSettingsValues>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemSettingsValues>> SetAsync(
        SystemSettingsValues request,
        CancellationToken cancellationToken)
    {
        return Ok(await settingsService.SetAsync(request, cancellationToken));
    }
}
