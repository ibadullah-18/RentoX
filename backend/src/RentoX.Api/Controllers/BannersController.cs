using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Content;
using RentoX.Domain.Users.Enums;

namespace RentoX.Api.Controllers;

/// <summary>The banners at the top of the home page.</summary>
[ApiController]
[Route("api/banners")]
public sealed class BannersController(IHomeBannerService bannerService) : ControllerBase
{
    [HttpGet]
    [ResponseCache(Duration = 60)]
    [ProducesResponseType<IReadOnlyList<PublicBannerItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PublicBannerItem>>> GetAsync(
        [FromQuery] string language = "az",
        CancellationToken cancellationToken = default)
    {
        PreferredLanguage? preferred = language.ToLowerInvariant() switch
        {
            "az" => PreferredLanguage.Azerbaijani,
            "ru" => PreferredLanguage.Russian,
            "en" => PreferredLanguage.English,
            _ => null
        };
        if (preferred is null)
        {
            return BadRequest("Language must be az, ru or en.");
        }

        return Ok(await bannerService.GetLiveAsync(preferred.Value, cancellationToken));
    }

    [HttpGet("{id:guid}/image")]
    [ResponseCache(Duration = 300)]
    public async Task<IActionResult> ImageAsync(Guid id, CancellationToken cancellationToken)
    {
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        BannerImageContent? image = await bannerService.OpenImageAsync(id, cancellationToken);
        return image is null ? NotFound() : File(image.Content, image.ContentType, enableRangeProcessing: true);
    }
}
