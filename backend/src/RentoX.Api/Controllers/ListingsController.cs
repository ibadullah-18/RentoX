using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Common;
using RentoX.Application.Listings;
using RentoX.Contracts.Common;
using RentoX.Contracts.Listings;
using RentoX.Domain.Users.Enums;
using System.Security.Claims;

namespace RentoX.Api.Controllers;

[ApiController]
[Route("api/listings")]
public sealed class ListingsController(
    IListingCreationService listingCreationService,
    IListingImageService listingImageService,
    IListingImageManagementService imageManagementService,
    IListingQueryService listingQueryService,
    IListingUpdateService listingUpdateService,
    IListingFieldUpdateService listingFieldUpdateService)
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

    [Authorize]
    [HttpGet("mine")]
    [ProducesResponseType<
    PagedResponse<OwnedListingSummaryResponse>>(
    StatusCodes.Status200OK)]
    public async Task<ActionResult<
    PagedResponse<OwnedListingSummaryResponse>>>
    GetMineAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetOwnerId(out Guid ownerId))
        {
            return Unauthorized();
        }

        PagedResult<OwnedListingSummaryResult> result =
            await listingQueryService.GetMineAsync(
                ownerId,
                page,
                pageSize,
                cancellationToken);

        OwnedListingSummaryResponse[] items =
            result.Items
                .Select(item =>
                    new OwnedListingSummaryResponse(
                        item.Id,
                        item.CategoryId,
                        item.Title,
                        item.Price,
                        item.Currency,
                        item.RentalPeriodUnit,
                        item.Status,
                        item.CoverImageId.HasValue
                            ? $"/api/listing-images/{item.CoverImageId}"
                            : null,
                        item.ImageCount,
                        item.CreatedAtUtc,
                        item.PublishedAtUtc,
                        item.ExpiresAtUtc))
                .ToArray();

        return Ok(new PagedResponse<
            OwnedListingSummaryResponse>(
                items,
                result.Page,
                result.PageSize,
                result.TotalCount,
                result.TotalPages));
    }

    [Authorize]
    [HttpGet("mine/{listingId:guid}")]
    [ProducesResponseType<OwnedListingDetailsResponse>(
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
    StatusCodes.Status404NotFound)]
    public async Task<ActionResult<
    OwnedListingDetailsResponse>>
    GetMineByIdAsync(
        Guid listingId,
        [FromQuery] string language = "az",
        CancellationToken cancellationToken = default)
    {
        if (!TryGetOwnerId(out Guid ownerId))
        {
            return Unauthorized();
        }

        PreferredLanguage? preferredLanguage =
            ParseLanguage(language);

        if (!preferredLanguage.HasValue)
        {
            return BadRequest(
                "Language must be az, ru or en.");
        }

        OwnedListingDetailsResult? result =
            await listingQueryService.GetMineByIdAsync(
                ownerId,
                listingId,
                preferredLanguage.Value,
                cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        ListingImageItemResponse[] images =
            result.Images
                .Select(image =>
                    new ListingImageItemResponse(
                        image.Id,
                        $"/api/listing-images/{image.Id}",
                        image.DisplayOrder,
                        image.IsCover))
                .ToArray();

        ListingFieldValueDetailsResponse[] fields =
            result.Fields
                .Select(field =>
                    new ListingFieldValueDetailsResponse(
                        field.FieldId,
                        field.Key,
                        field.Label,
                        field.Type,
                        field.TextValue,
                        field.NumericValue,
                        field.FlagValue,
                        field.CalendarValue,
                        field.CustomValue,
                        field.Selections
                            .Select(selection =>
                                new ListingFieldSelectionValueResponse(
                                    selection.OptionId,
                                    selection.Value,
                                    selection.Label))
                            .ToArray()))
                .ToArray();

        return Ok(new OwnedListingDetailsResponse(
            result.Id,
            result.OwnerId,
            result.CategoryId,
            result.Title,
            result.Description,
            result.Price,
            result.Currency,
            result.RentalPeriodUnit,
            result.Status,
            result.RejectionReason,
            result.CreatedAtUtc,
            result.UpdatedAtUtc,
            result.PublishedAtUtc,
            result.ExpiresAtUtc,
            images,
            fields));
    }

    [Authorize]
    [HttpPut("{listingId:guid}")]
    [ProducesResponseType<UpdateListingResponse>(
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
    StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UpdateListingResponse>>
    UpdateAsync(
        Guid listingId,
        UpdateListingRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out Guid ownerId))
        {
            return Unauthorized();
        }

        UpdateListingCommand command = new(
            ownerId,
            listingId,
            request.Title,
            request.Description,
            request.Price,
            request.Currency,
            request.RentalPeriodUnit);

        UpdateListingResult result =
            await listingUpdateService.UpdateAsync(
                command,
                cancellationToken);

        return Ok(new UpdateListingResponse(
            result.Id,
            result.OwnerId,
            result.CategoryId,
            result.Title,
            result.Description,
            result.Price,
            result.Currency,
            result.RentalPeriodUnit,
            result.Status,
            result.UpdatedAtUtc));
    }

    [Authorize]
    [HttpPut("{listingId:guid}/fields")]
    [ProducesResponseType<UpdateListingFieldsResponse>(
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
    StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<
    UpdateListingFieldsResponse>>
    UpdateFieldsAsync(
        Guid listingId,
        UpdateListingFieldsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out Guid ownerId))
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

        UpdateListingFieldsCommand command = new(
            ownerId,
            listingId,
            fields);

        UpdateListingFieldsResult result =
            await listingFieldUpdateService.UpdateAsync(
                command,
                cancellationToken);

        return Ok(new UpdateListingFieldsResponse(
            result.ListingId,
            result.FieldCount,
            result.Status,
            result.UpdatedAtUtc));
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

    private static PreferredLanguage? ParseLanguage(
    string language)
    {
        return language.Trim().ToLowerInvariant() switch
        {
            "az" => PreferredLanguage.Azerbaijani,
            "ru" => PreferredLanguage.Russian,
            "en" => PreferredLanguage.English,
            _ => null
        };
    }
}