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
    [ResponseCache(
        NoStore = true,
        Location = ResponseCacheLocation.None)]
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

        return File(
            result.Content,
            result.ContentType,
            enableRangeProcessing: true);
    }
}
