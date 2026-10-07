using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RentoX.Api.Authentication;
using RentoX.Application.Accounts;
using RentoX.Contracts.Accounts;

namespace RentoX.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class ProfileImagesController(
    IProfileImageService profileImageService)
    : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("users/{userId:guid}/profile-image")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> OpenAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store, no-cache";
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        ProfileImageContentResult? result =
            await profileImageService.OpenAsync(
                userId,
                cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return File(
            result.Content,
            result.ContentType,
            enableRangeProcessing: true);
    }

    [Authorize]
    [HttpPost("account/me/profile-image")]
    [EnableRateLimiting("image-upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    [ProducesResponseType<ProfileImageResponse>(
        StatusCodes.Status201Created)]
    public async Task<IActionResult> UploadAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        Guid? userId = AuthenticatedUserId.Resolve(User);

        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        if (file is null || file.Length is <= 0 or > 5 * 1024 * 1024)
        {
            return BadRequest(
                "Profile image must be between 1 byte and 5 MB.");
        }

        await using Stream content = file.OpenReadStream();

        ProfileImageResult result =
            await profileImageService.UploadAsync(
                new UploadProfileImageCommand(
                    userId.Value,
                    content,
                    file.FileName,
                    file.ContentType,
                    file.Length),
                cancellationToken);

        return Created(
            result.Url,
            new ProfileImageResponse(result.UserId, result.Url));
    }

    [Authorize]
    [HttpDelete("account/me/profile-image")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(
        CancellationToken cancellationToken)
    {
        Guid? userId = AuthenticatedUserId.Resolve(User);

        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        await profileImageService.DeleteAsync(
            userId.Value,
            cancellationToken);

        return NoContent();
    }
}