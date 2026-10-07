using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Stores;

namespace RentoX.Api.Controllers;

[ApiController]
[Route("api/stores")]
public sealed class StoreImagesController(
    IStoreImageService storeImageService,
    ICurrentUserContext currentUserContext,
    IAuthorizationService authorizationService)
    : ControllerBase
{
    [HttpGet("{storeId:guid}/logo")]
    public Task<IActionResult> GetLogoAsync(
        Guid storeId,
        CancellationToken cancellationToken)
    {
        return OpenAsync(
            storeId,
            StoreImageKind.Logo,
            cancellationToken);
    }

    [HttpGet("{storeId:guid}/cover")]
    public Task<IActionResult> GetCoverAsync(
        Guid storeId,
        CancellationToken cancellationToken)
    {
        return OpenAsync(
            storeId,
            StoreImageKind.Cover,
            cancellationToken);
    }

    [Authorize]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("image-upload")]
    [HttpPost("mine/logo")]
    [Consumes("multipart/form-data")]
    public Task<IActionResult> UploadLogoAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        return UploadAsync(
            StoreImageKind.Logo,
            file,
            cancellationToken);
    }

    [Authorize]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("image-upload")]
    [HttpPost("mine/cover")]
    [Consumes("multipart/form-data")]
    public Task<IActionResult> UploadCoverAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        return UploadAsync(
            StoreImageKind.Cover,
            file,
            cancellationToken);
    }

    [Authorize]
    [HttpDelete("mine/logo")]
    public Task<IActionResult> DeleteLogoAsync(
        CancellationToken cancellationToken)
    {
        return DeleteAsync(
            StoreImageKind.Logo,
            cancellationToken);
    }

    [Authorize]
    [HttpDelete("mine/cover")]
    public Task<IActionResult> DeleteCoverAsync(
        CancellationToken cancellationToken)
    {
        return DeleteAsync(
            StoreImageKind.Cover,
            cancellationToken);
    }

    private async Task<IActionResult> OpenAsync(
    Guid storeId,
    StoreImageKind kind,
    CancellationToken cancellationToken)
    {
        Guid? viewerUserId =
            RentoX.Api.Authentication.AuthenticatedUserId.Resolve(User);

        bool canModerate = false;

        if (viewerUserId.HasValue)
        {
            AuthorizationResult authorization =
                await authorizationService.AuthorizeAsync(
                    User,
                    RentoX.Application.Authorization.PolicyNames.AdminAccess);

            canModerate = authorization.Succeeded;
        }

        StoreImageContentResult? result =
            await storeImageService.OpenAsync(
                storeId,
                kind,
                viewerUserId,
                canModerate,
                cancellationToken);

        if (result is null)
        {
            Response.Headers.CacheControl =
                "no-store, no-cache";

            return NotFound();
        }

        Response.Headers.CacheControl =
            "no-store, no-cache";

        Response.Headers["X-Content-Type-Options"] =
            "nosniff";

        return File(
            result.Content,
            result.ContentType,
            enableRangeProcessing: true);
    }

    private async Task<IActionResult> UploadAsync(
        StoreImageKind kind,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        Guid ownerId = GetRequiredUserId();

        await using Stream content =
            file.OpenReadStream();

        UploadStoreImageCommand command = new(
            ownerId,
            kind,
            content,
            file.FileName,
            file.ContentType,
            file.Length);

        StoreImageResult result =
            await storeImageService.UploadAsync(
                command,
                cancellationToken);

        string url =
            kind == StoreImageKind.Logo
                ? $"/api/stores/{result.StoreId}/logo"
                : $"/api/stores/{result.StoreId}/cover";

        return Created(url, new
        {
            result.StoreId,
            Kind = (int)result.Kind,
            Url = url
        });
    }

    private async Task<IActionResult> DeleteAsync(
        StoreImageKind kind,
        CancellationToken cancellationToken)
    {
        Guid ownerId = GetRequiredUserId();

        await storeImageService.DeleteAsync(
            ownerId,
            kind,
            cancellationToken);

        return NoContent();
    }

    private Guid GetRequiredUserId()
    {
        if (!currentUserContext.IsAuthenticated ||
            !currentUserContext.UserId.HasValue)
        {
            throw new UnauthorizedAccessException(
                "Authentication is required.");
        }

        return currentUserContext.UserId.Value;
    }
}
