using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Api.Authentication;
using RentoX.Application.Authorization;
using RentoX.Application.Listings;

namespace RentoX.Api.Controllers;

[ApiController]
[Route("api/listing-images")]
public sealed class ListingImagesController(
    IListingImageManagementService imageService,
    IAuthorizationService authorizationService)
    : ControllerBase
{
    [HttpGet("{imageId:guid}")]
    [ProducesResponseType(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(
        Guid imageId,
        CancellationToken cancellationToken)
    {
        Guid? viewerUserId =
            AuthenticatedUserId.Resolve(User);

        bool canModerate = false;

        if (viewerUserId.HasValue)
        {
            AuthorizationResult authorization =
                await authorizationService.AuthorizeAsync(
                    User,
                    PolicyNames.AdminAccess);

            canModerate = authorization.Succeeded;
        }

        ListingImageContentResult? result =
            await imageService.OpenAsync(
                imageId,
                viewerUserId,
                canModerate,
                cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        Response.Headers["X-Content-Type-Options"] = "nosniff";

        // An image never changes under its id (a new photo is a new id), so
        // the photos of a live listing may be kept by browsers and the CDN.
        // One day only: a listing that is hidden, deleted or removed by a
        // moderator must stop showing its photos soon, and nothing purges
        // the CDN yet. Anything not public stays private.
        Response.Headers.CacheControl = result.IsPublic
            ? "public, max-age=86400"
            : "private, no-store";

        return File(
            result.Content,
            result.ContentType,
            enableRangeProcessing: true);
    }
}
