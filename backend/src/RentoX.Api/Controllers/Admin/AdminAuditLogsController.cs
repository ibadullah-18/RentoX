using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Auditing;
using RentoX.Application.Authorization;
using RentoX.Contracts.Auditing;

namespace RentoX.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/audit-logs")]
[Authorize(Policy = PolicyNames.AdminAccess, Roles = "SuperAdmin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminAuditLogsController(
    IAuditLogQueryService auditLogQueryService)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(AuditLogPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuditLogPageResponse>> SearchAsync(
        [FromQuery] SearchAuditLogsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        AuditLogPageResult result =
            await auditLogQueryService.SearchAsync(
                new AuditLogSearchQuery(
                    request.ActorUserId,
                    request.TargetId,
                    request.OperationId,
                    request.Action,
                    request.FromUtc,
                    request.ToUtc,
                    request.Page,
                    request.PageSize),
                cancellationToken);

        AuditLogItemResponse[] items =
            result.Items
                .Select(item => new AuditLogItemResponse(
                    item.Id,
                    item.OperationId,
                    item.ActorUserId,
                    item.TargetId,
                    item.Action,
                    item.PreviousValue,
                    item.CurrentValue,
                    item.RelatedEntityId,
                    item.OccurredAtUtc))
                .ToArray();

        return Ok(new AuditLogPageResponse(
            items,
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages,
            result.FromUtc,
            result.ToUtc));
    }
}