using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RentoX.Application.Authorization;
using RentoX.Application.Content;
using RentoX.Domain.Content;
using RentoX.Infrastructure.Content;

namespace RentoX.Api.Controllers.Admin;

/// <summary>The name, the logos and the icon of the brand. The owner (SuperAdmin) and any Admin given the branding section change them.</summary>
[ApiController]
[Route("api/admin/branding")]
[Authorize(Policy = PolicyNames.AdminAccess)]
[Authorize(Policy = AdminSections.BrandingPolicy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminBrandingController(IBrandingService brandingService) : ControllerBase
{
    private const long MaximumImageSize = 5 * 1024 * 1024;

    [HttpGet]
    [ProducesResponseType<BrandingPublic>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BrandingPublic>> GetAsync(CancellationToken cancellationToken) =>
        Ok(await brandingService.GetAsync(cancellationToken));

    [HttpPut]
    [ProducesResponseType<BrandingPublic>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BrandingPublic>> UpdateAsync(BrandingValues values, CancellationToken cancellationToken) =>
        Ok(await brandingService.UpdateAsync(values, cancellationToken));

    [HttpPut("{slot}/image")]
    [EnableRateLimiting("image-upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaximumImageSize + 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaximumImageSize + 1024 * 1024)]
    [ProducesResponseType<BrandingPublic>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BrandingPublic>> SetImageAsync(string slot, IFormFile file, CancellationToken cancellationToken)
    {
        if (!BrandingService.TryParseSlot(slot, out BrandingImageSlot parsed))
        {
            return NotFound();
        }

        if (file is null || file.Length is <= 0 or > MaximumImageSize)
        {
            return BadRequest("The picture must be between 1 byte and 5 MB.");
        }

        await using Stream content = file.OpenReadStream();
        return Ok(await brandingService.SetImageAsync(
            parsed, content, file.FileName, file.ContentType, file.Length, cancellationToken));
    }

    [HttpDelete("{slot}/image")]
    [ProducesResponseType<BrandingPublic>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BrandingPublic>> ResetImageAsync(string slot, CancellationToken cancellationToken)
    {
        if (!BrandingService.TryParseSlot(slot, out BrandingImageSlot parsed))
        {
            return NotFound();
        }

        return Ok(await brandingService.ResetImageAsync(parsed, cancellationToken));
    }
}
