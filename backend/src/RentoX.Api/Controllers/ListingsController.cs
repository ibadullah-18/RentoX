using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Listings;
using RentoX.Contracts.Listings;

namespace RentoX.Api.Controllers;

[ApiController]
[Route("api/listings")]
public sealed class ListingsController(
    IListingCreationService listingCreationService,
    IListingImageService listingImageService,
    IListingImageManagementService imageManagementService)
    : ControllerBase
{
    [Authorize]
    [HttpPost]
    [ProducesResponseType<CreateListingResponse>(
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreateListingResponse>>
        CreateAsync(
            CreateListingRequest request,
            CancellationToken cancellationToken)
    {
        string? userIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(
                userIdValue,
                out Guid ownerId))
        {
            return Unauthorized();
        }

        CreateListingFieldInput[] fields =
            request.Fields
                .Select(field =>
                    new CreateListingFieldInput(
                        field.FieldId,
                        field.TextValue,
                        field.NumericValue,
                        field.FlagValue,
                        field.CalendarValue,
                        field.CustomValue,
                        field.OptionIds))
                .ToArray();

        CreateListingCommand command = new(
            ownerId,
            request.CategoryId,
            request.Title,
            request.Description,
            request.Price,
            request.Currency,
            request.RentalPeriodUnit,
            fields);

        CreateListingResult result =
            await listingCreationService.CreateAsync(
                command,
                cancellationToken);

        CreateListingResponse response = new(
            result.Id,
            result.OwnerId,
            result.CategoryId,
            result.Title,
            result.Price,
            result.Currency,
            result.RentalPeriodUnit,
            result.Status,
            result.CreatedAtUtc);

        return Created(
            $"/api/listings/{result.Id}",
            response);
    }

    [Authorize]
    [HttpPost("{listingId:guid}/images")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<ListingImageResponse>(
    StatusCodes.Status201Created)]
    [ProducesResponseType(
    StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
    StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ListingImageResponse>>
    UploadImageAsync(
        Guid listingId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        string? userIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(
                userIdValue,
                out Guid ownerId))
        {
            return Unauthorized();
        }

        await using Stream stream =
            file.OpenReadStream();

        UploadListingImageCommand command = new(
            ownerId,
            listingId,
            stream,
            file.FileName,
            file.ContentType,
            file.Length);

        ListingImageResult result =
            await listingImageService.UploadAsync(
                command,
                cancellationToken);

        ListingImageResponse response = new(
            result.Id,
            result.ListingId,
            $"/api/listing-images/{result.Id}",
            result.DisplayOrder,
            result.IsCover,
            result.SizeBytes);

        return Created(response.Url, response);
    }

    [Authorize]
    [HttpDelete(
    "{listingId:guid}/images/{imageId:guid}")]
    [ProducesResponseType(
    StatusCodes.Status204NoContent)]
    [ProducesResponseType(
    StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteImageAsync(
    Guid listingId,
    Guid imageId,
    CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out Guid ownerId))
        {
            return Unauthorized();
        }

        await imageManagementService.DeleteAsync(
            ownerId,
            listingId,
            imageId,
            cancellationToken);

        return NoContent();
    }

    [Authorize]
    [HttpPatch(
    "{listingId:guid}/images/{imageId:guid}/cover")]
    [ProducesResponseType(
    StatusCodes.Status204NoContent)]
    [ProducesResponseType(
    StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetCoverImageAsync(
    Guid listingId,
    Guid imageId,
    CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out Guid ownerId))
        {
            return Unauthorized();
        }

        await imageManagementService.SetCoverAsync(
            ownerId,
            listingId,
            imageId,
            cancellationToken);

        return NoContent();
    }

    [Authorize]
    [HttpPut("{listingId:guid}/images/order")]
    [ProducesResponseType(
    StatusCodes.Status204NoContent)]
    [ProducesResponseType(
    StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
    StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ReorderImagesAsync(
    Guid listingId,
    ReorderListingImagesRequest request,
    CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out Guid ownerId))
        {
            return Unauthorized();
        }

        await imageManagementService.ReorderAsync(
            ownerId,
            listingId,
            request.ImageIds,
            cancellationToken);

        return NoContent();
    }

    private bool TryGetOwnerId(out Guid ownerId)
    {
        string? userIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        return Guid.TryParse(
            userIdValue,
            out ownerId);
    }
}