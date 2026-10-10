using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RentoX.Application.Moderation;
using RentoX.Contracts.Moderation;

namespace RentoX.Api.Controllers;

/// <summary>Users report a listing or a store they find wrong.</summary>
[ApiController]
[Authorize]
public sealed class ContentReportsController(
    IContentReportService reportService)
    : ControllerBase
{
    private const int ListingTarget = 1;
    private const int StoreTarget = 2;

    [EnableRateLimiting("content-report")]
    [HttpPost("api/listings/{listingId:guid}/reports")]
    [ProducesResponseType<ContentReportResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<ContentReportResponse>> ReportListingAsync(
        Guid listingId,
        CreateContentReportRequest request,
        CancellationToken cancellationToken) =>
        CreateAsync(ListingTarget, listingId, request, cancellationToken);

    [EnableRateLimiting("content-report")]
    [HttpPost("api/stores/{storeId:guid}/reports")]
    [ProducesResponseType<ContentReportResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<ContentReportResponse>> ReportStoreAsync(
        Guid storeId,
        CreateContentReportRequest request,
        CancellationToken cancellationToken) =>
        CreateAsync(StoreTarget, storeId, request, cancellationToken);

    private async Task<ActionResult<ContentReportResponse>> CreateAsync(
        int targetType,
        Guid targetId,
        CreateContentReportRequest request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out Guid userId) ||
            userId == Guid.Empty)
        {
            return Unauthorized();
        }

        ContentReportResult? result =
            await reportService.CreateAsync(
                userId,
                targetType,
                targetId,
                request.Reason,
                request.Details,
                cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Created(
            string.Empty,
            new ContentReportResponse(
                result.Id,
                result.TargetType,
                result.TargetId,
                result.Reason,
                result.Details,
                result.Status,
                result.CreatedAtUtc));
    }
}
