using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Listings;

namespace RentoX.Api.Controllers;

[ApiController]
[Route("api/listing-images")]
public sealed class ListingImagesController(
    IListingImageManagementService imageService)
    : ControllerBase
{
    [HttpGet("{imageId:guid}")]
    [ResponseCache(
        Duration = 86400,
        Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(
        Guid imageId,
        CancellationToken cancellationToken)
    {
        ListingImageContentResult? result =
            await imageService.OpenAsync(
                imageId,
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
}