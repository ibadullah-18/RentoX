using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Authorization;
using RentoX.Application.Common;
using RentoX.Application.Listings;
using RentoX.Contracts.Common;
using RentoX.Contracts.Listings;
using RentoX.Domain.Users.Enums;
using System.Security.Cryptography.X509Certificates;

namespace RentoX.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/listings")]
[Authorize(Policy = PolicyNames.AdminAccess)]
public sealed class AdminListingsController(
    IListingModerationService moderationService,
    IListingModerationQueryService moderationQueryService)
    : ControllerBase
{
    [HttpGet("pending")]
    [ProducesResponseType<
        PagedResponse<ModerationListingSummaryResponse>>(
        StatusCodes.Status200OK)]
    public async Task<ActionResult<
        PagedResponse<ModerationListingSummaryResponse>>>
        GetPendingAsync(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
    {
        PagedResult<ModerationListingSummaryResult> result =
            await moderationService.GetPendingAsync(
                page,
                pageSize,
                cancellationToken);

        ModerationListingSummaryResponse[] items =
            result.Items
                .Select(item =>
                    new ModerationListingSummaryResponse(
                        item.Id,
                        item.OwnerId,
                        item.CategoryId,
                        item.Title,
                        item.Price,
                        item.Currency,
                        item.RentalPeriodUnit,
                        item.CoverImageId.HasValue
                            ? $"/api/listing-images/{item.CoverImageId}"
                            : null,
                        item.ImageCount,
                        item.CreatedAtUtc,
                        item.UpdatedAtUtc))
                .ToArray();

        return Ok(new PagedResponse<
            ModerationListingSummaryResponse>(
                items,
                result.Page,
                result.PageSize,
                result.TotalCount,
                result.TotalPages));
    }

    [HttpPost("{listingId:guid}/approve")]
    [ProducesResponseType<ListingModerationResponse>(
        StatusCodes.Status200OK)]
    public async Task<ActionResult<
        ListingModerationResponse>>
        ApproveAsync(
            Guid listingId,
            CancellationToken cancellationToken)
    {
        ListingModerationResult result =
            await moderationService.ApproveAsync(
                listingId,
                cancellationToken);

        return Ok(MapResponse(result));
    }

    [HttpPost("{listingId:guid}/reject")]
    [ProducesResponseType<ListingModerationResponse>(
        StatusCodes.Status200OK)]
    public async Task<ActionResult<
        ListingModerationResponse>>
        RejectAsync(
            Guid listingId,
            RejectListingRequest request,
            CancellationToken cancellationToken)
    {
        ListingModerationResult result =
            await moderationService.RejectAsync(
                listingId,
                request.Reason,
                cancellationToken);

        return Ok(MapResponse(result));
    }

    [HttpGet("{listingId:guid}")]
    [ProducesResponseType<ModerationListingDetailsResponse>(
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
    StatusCodes.Status404NotFound)]
    public async Task<ActionResult<
    ModerationListingDetailsResponse>>
    GetByIdAsync(
        Guid listingId,
        [FromQuery] string language = "az",
        CancellationToken cancellationToken = default)
    {
        PreferredLanguage? preferredLanguage =
            ParseLanguage(language);

        if (!preferredLanguage.HasValue)
        {
            return BadRequest(
                "Language must be az, ru or en.");
        }

        ModerationListingDetailsResult? result =
            await moderationQueryService.GetByIdAsync(
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

        return Ok(new ModerationListingDetailsResponse(
            result.Id,
            result.OwnerId,
            result.OwnerFullName,
            result.PhoneNumber,
            result.CategoryId,
            result.Title,
            result.Description,
            result.Price,
            result.Currency,
            result.RentalPeriodUnit,
            result.Status,
            result.CreatedAtUtc,
            result.UpdatedAtUtc,
            images,
            fields));
    }

    private static ListingModerationResponse MapResponse(
        ListingModerationResult result)
    {
        return new ListingModerationResponse(
            result.ListingId,
            result.Status,
            result.RejectionReason,
            result.PublishedAtUtc,
            result.ExpiresAtUtc,
            result.UpdatedAtUtc);
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