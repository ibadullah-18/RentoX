using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Content;
using RentoX.Domain.Users.Enums;

namespace RentoX.Api.Controllers;

/// <summary>The fixed pages (terms, privacy, about, …) and the contact details, for the site and the app.</summary>
[ApiController]
[Route("api")]
public sealed class PagesController(
    IContentPageService pageService,
    ISiteInfoService siteInfoService)
    : ControllerBase
{
    [HttpGet("pages")]
    [ResponseCache(Duration = 60)]
    [ProducesResponseType<IReadOnlyList<PublicPageSummary>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PublicPageSummary>>> ListAsync(
        [FromQuery] string language = "az",
        CancellationToken cancellationToken = default)
    {
        PreferredLanguage? preferred = Parse(language);
        if (preferred is null)
        {
            return BadRequest("Language must be az, ru or en.");
        }

        return Ok(await pageService.ListPublicAsync(preferred.Value, cancellationToken));
    }

    [HttpGet("pages/{slug}")]
    [ResponseCache(Duration = 60)]
    [ProducesResponseType<PublicPageResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicPageResult>> GetAsync(
        string slug,
        [FromQuery] string language = "az",
        CancellationToken cancellationToken = default)
    {
        PreferredLanguage? preferred = Parse(language);
        if (preferred is null)
        {
            return BadRequest("Language must be az, ru or en.");
        }

        PublicPageResult? page = await pageService.GetPublicAsync(slug, preferred.Value, cancellationToken);
        return page is null ? NotFound() : Ok(page);
    }

    [HttpGet("site-info")]
    [ResponseCache(Duration = 60)]
    [ProducesResponseType<SiteInfoValues>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SiteInfoValues>> SiteInfoAsync(CancellationToken cancellationToken)
    {
        return Ok(await siteInfoService.GetAsync(cancellationToken));
    }

    private static PreferredLanguage? Parse(string language) =>
        language.ToLowerInvariant() switch
        {
            "az" => PreferredLanguage.Azerbaijani,
            "ru" => PreferredLanguage.Russian,
            "en" => PreferredLanguage.English,
            _ => null
        };
}
