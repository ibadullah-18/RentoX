using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Authorization;
using RentoX.Application.Content;

namespace RentoX.Api.Controllers.Admin;

/// <summary>Editing the fixed pages and the contact details. Needs the catalog section.</summary>
[ApiController]
[Route("api/admin")]
[Authorize(Policy = PolicyNames.AdminAccess)]
[Authorize(Policy = AdminSections.CatalogPolicy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminContentController(
    IContentPageService pageService,
    ISiteInfoService siteInfoService)
    : ControllerBase
{
    [HttpGet("pages")]
    [ProducesResponseType<IReadOnlyList<AdminPageItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminPageItem>>> ListAsync(CancellationToken cancellationToken)
    {
        return Ok(await pageService.ListAdminAsync(cancellationToken));
    }

    [HttpPut("pages/{slug}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAsync(string slug, PageInput input, CancellationToken cancellationToken)
    {
        await pageService.UpdateAsync(slug, input, cancellationToken);
        return NoContent();
    }

    [HttpGet("site-info")]
    [ProducesResponseType<SiteInfoValues>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SiteInfoValues>> GetSiteInfoAsync(CancellationToken cancellationToken)
    {
        return Ok(await siteInfoService.GetAsync(cancellationToken));
    }

    [HttpPut("site-info")]
    [ProducesResponseType<SiteInfoValues>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SiteInfoValues>> UpdateSiteInfoAsync(
        SiteInfoValues values, CancellationToken cancellationToken)
    {
        return Ok(await siteInfoService.UpdateAsync(values, cancellationToken));
    }
}
