using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RentoX.Application.Authorization;
using RentoX.Application.Content;

namespace RentoX.Api.Controllers.Admin;

/// <summary>The banners of the home page. Needs the catalog section.</summary>
[ApiController]
[Route("api/admin/banners")]
[Authorize(Policy = PolicyNames.AdminAccess)]
[Authorize(Policy = AdminSections.CatalogPolicy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminBannersController(IHomeBannerService bannerService) : ControllerBase
{
    private const long MaximumImageSize = 5 * 1024 * 1024;

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AdminBannerItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminBannerItem>>> ListAsync(CancellationToken cancellationToken)
    {
        return Ok(await bannerService.ListAsync(cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<CreatedBanner>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CreatedBanner>> CreateAsync(BannerInput input, CancellationToken cancellationToken)
    {
        return Ok(new CreatedBanner(await bannerService.CreateAsync(input, cancellationToken)));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAsync(Guid id, BannerInput input, CancellationToken cancellationToken)
    {
        await bannerService.UpdateAsync(id, input, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/image")]
    [EnableRateLimiting("image-upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaximumImageSize + 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaximumImageSize + 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetImageAsync(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length is <= 0 or > MaximumImageSize)
        {
            return BadRequest("The banner picture must be between 1 byte and 5 MB.");
        }

        await using Stream content = file.OpenReadStream();
        await bannerService.SetImageAsync(id, content, file.FileName, file.ContentType, file.Length, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        return await bannerService.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
    }
}

public sealed record CreatedBanner(Guid Id);
