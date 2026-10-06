using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Messaging;
using RentoX.Contracts.Messaging;

namespace RentoX.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/conversations/{conversationId:guid}/reports")]
public sealed class ConversationReportsController(IConversationReportService reportService)
    : ControllerBase
{
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("conversation-report")]
    [HttpPost]
    [ProducesResponseType<ConversationReportResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConversationReportResponse>> CreateAsync(
        Guid conversationId,
        CreateConversationReportRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out Guid userId)) { return Unauthorized(); }

        ConversationReportResult? result = await reportService.CreateAsync(
            new CreateConversationReportCommand(
                userId, conversationId, request.Reason,
                request.Details, request.EvidenceMessageId),
            cancellationToken);
        if (result is null) { return NotFound(); }

        return Created(
            $"/api/conversations/{conversationId:D}/reports/mine",
            Map(result));
    }

    [HttpGet("mine")]
    [ProducesResponseType<IReadOnlyList<ConversationReportResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ConversationReportResponse>>> GetMineAsync(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out Guid userId)) { return Unauthorized(); }
        IReadOnlyList<ConversationReportResult>? result = await reportService.GetMineAsync(
            userId, conversationId, cancellationToken);
        if (result is null) { return NotFound(); }
        return Ok(result.Select(Map).ToArray());
    }

    private bool TryGetUserId(out Guid userId)
    {
        return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId)
            && userId != Guid.Empty;
    }

    private static ConversationReportResponse Map(ConversationReportResult result)
    {
        return new ConversationReportResponse(
            result.Id, result.ConversationId, result.ReporterId,
            result.Reason, result.Details, result.EvidenceMessageId,
            result.Status, result.CreatedAtUtc,
            result.ReviewedByUserId, result.ReviewedAtUtc,
            result.ResolutionNote);
    }
}
