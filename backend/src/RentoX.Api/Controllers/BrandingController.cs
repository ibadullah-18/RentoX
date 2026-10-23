using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Content;
using RentoX.Domain.Content;
using RentoX.Infrastructure.Content;

namespace RentoX.Api.Controllers;

/// <summary>The look of the brand for the site and the apps: its name, logos and icon.</summary>
[ApiController]
[Route("api/branding")]
public sealed class BrandingController(IBrandingService brandingService) : ControllerBase
{
    [HttpGet]
    [ResponseCache(Duration = 60)]
    [ProducesResponseType<BrandingPublic>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BrandingPublic>> GetAsync(CancellationToken cancellationToken)
    {
        return Ok(await brandingService.GetAsync(cancellationToken));
    }

    /// <summary>One picture. The address carries ?v=, so a changed picture is a new address and may be kept for long.</summary>
    [HttpGet("{slot}/image")]
    [ResponseCache(Duration = 86400)]
    public async Task<IActionResult> ImageAsync(string slot, CancellationToken cancellationToken)
    {
        if (!BrandingService.TryParseSlot(slot, out BrandingImageSlot parsed))
        {
            return NotFound();
        }

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        BrandingImageContent? image = await brandingService.OpenImageAsync(parsed, cancellationToken);
        return image is null ? NotFound() : File(image.Content, image.ContentType, enableRangeProcessing: true);
    }
}
