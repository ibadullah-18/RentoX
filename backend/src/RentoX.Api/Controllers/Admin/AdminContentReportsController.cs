using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Authorization;
using RentoX.Application.Common;
using RentoX.Application.Moderation;
using RentoX.Contracts.Common;
using RentoX.Contracts.Moderation;

namespace RentoX.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/content-reports")]
[Authorize(Policy = PolicyNames.AdminAccess)]
public sealed class AdminContentReportsController(
    IContentReportService reportService)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ContentReportSummaryResponse>>>
        GetAsync(
            [FromQuery] int? status = null,
            [FromQuery] int? targetType = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
    {
        PagedResult<ContentReportSummaryResult> result =
            await reportService.GetForAdminAsync(
                status,
                targetType,
                page,
                pageSize,
                cancellationToken);

        return Ok(new PagedResponse<ContentReportSummaryResponse>(
            result.Items
                .Select(item => new ContentReportSummaryResponse(
                    item.Id,
                    item.TargetType,
                    item.TargetId,
                    item.TargetTitle,
                    item.ReporterId,
                    item.Reason,
                    item.Status,
                    item.CreatedAtUtc))
                .ToArray(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages));
    }

    [HttpGet("{reportId:guid}")]
    [ProducesResponseType<ContentReportDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentReportDetailsResponse>> GetDetailsAsync(
        Guid reportId,
        CancellationToken cancellationToken)
    {
        ContentReportDetailsResult? item =
            await reportService.GetDetailsAsync(reportId, cancellationToken);

        if (item is null)
        {
            return NotFound();
        }

        return Ok(new ContentReportDetailsResponse(
            item.Id,
            item.TargetType,
            item.TargetId,
            item.TargetTitle,
            item.TargetOwnerId,
            item.ReporterId,
            item.Reason,
            item.Details,
            item.Status,
            item.CreatedAtUtc,
            item.ReviewedByUserId,
            item.ReviewedAtUtc,
            item.ResolutionNote,
            item.OpenReportsOnTarget));
    }

    [HttpPut("{reportId:guid}")]
    [ProducesResponseType<ContentReportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentReportResponse>> ReviewAsync(
        Guid reportId,
        ReviewContentReportRequest request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out Guid reviewerId) ||
            reviewerId == Guid.Empty)
        {
            return Unauthorized();
        }

        ContentReportResult? result =
            await reportService.ReviewAsync(
                reviewerId,
                reportId,
                request.Status,
                request.ResolutionNote,
                cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(new ContentReportResponse(
            result.Id,
            result.TargetType,
            result.TargetId,
            result.Reason,
            result.Details,
            result.Status,
            result.CreatedAtUtc));
    }
}
