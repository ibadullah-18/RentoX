using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Authorization;
using RentoX.Application.Common;
using RentoX.Application.Messaging;
using RentoX.Contracts.Common;
using RentoX.Contracts.Messaging;

namespace RentoX.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/conversation-reports")]
[Authorize(Policy = PolicyNames.AdminAccess)]
public sealed class AdminConversationReportsController(
    IConversationReportService reportService)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ConversationReportSummaryResponse>>> GetAsync(
        [FromQuery] int? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        PagedResult<ConversationReportSummaryResult> result =
            await reportService.GetForAdminAsync(status, page, pageSize, cancellationToken);
        return Ok(new PagedResponse<ConversationReportSummaryResponse>(
            result.Items.Select(item => new ConversationReportSummaryResponse(
                item.Id, item.ConversationId, item.ListingId, item.ListingTitle,
                item.ReporterId, item.OtherUserId, item.Reason,
                item.Status, item.CreatedAtUtc)).ToArray(),
            result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }

    [HttpGet("{reportId:guid}")]
    [ProducesResponseType<ConversationReportDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConversationReportDetailsResponse>> GetDetailsAsync(
        Guid reportId,
        CancellationToken cancellationToken)
    {
        ConversationReportDetailsResult? result =
            await reportService.GetDetailsAsync(reportId, cancellationToken);
        return result is null ? NotFound() : Ok(MapDetails(result));
    }

    [HttpPut("{reportId:guid}")]
    [ProducesResponseType<ConversationReportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConversationReportResponse>> ReviewAsync(
        Guid reportId,
        ReviewConversationReportRequest request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid reviewerId) ||
            reviewerId == Guid.Empty)
        {
            return Unauthorized();
        }

        ConversationReportResult? result = await reportService.ReviewAsync(
            reviewerId, reportId, request.Status,
            request.ResolutionNote, cancellationToken);
        return result is null ? NotFound() : Ok(MapReport(result));
    }

    [HttpGet("{reportId:guid}/images/{imageId:guid}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> OpenImageAsync(
        Guid reportId,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        MessageImageDownload? image = await reportService.OpenEvidenceImageAsync(
            reportId, imageId, cancellationToken);
        if (image is null) { return NotFound(); }
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(image.Content, image.ContentType);
    }

    private static ConversationReportDetailsResponse MapDetails(
        ConversationReportDetailsResult result)
    {
        return new ConversationReportDetailsResponse(
            MapReport(result.Report),
            result.ListingId,
            result.ListingTitle,
            result.BuyerId,
            result.SellerId,
            result.Messages.Select(message => new ConversationReportMessageResponse(
                message.Id, message.SenderId, message.Body,
                message.SentAtUtc, message.ReadAtUtc,
                message.Images.Select(image => new MessageImageResponse(
                    image.Id, image.Url, image.ContentType,
                    image.SizeBytes, image.DisplayOrder)).ToArray())).ToArray());
    }

    private static ConversationReportResponse MapReport(ConversationReportResult result)
    {
        return new ConversationReportResponse(
            result.Id, result.ConversationId, result.ReporterId,
            result.Reason, result.Details, result.EvidenceMessageId,
            result.Status, result.CreatedAtUtc,
            result.ReviewedByUserId, result.ReviewedAtUtc,
            result.ResolutionNote);
    }
}
