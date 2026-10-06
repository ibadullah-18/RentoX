using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Common;
using RentoX.Application.Notifications;
using RentoX.Contracts.Common;
using RentoX.Contracts.Notifications;

namespace RentoX.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController(INotificationService notificationService)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<NotificationResponse>>> GetAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out Guid userId)) { return Unauthorized(); }
        PagedResult<NotificationResult> result = await notificationService.GetAsync(
            userId, page, pageSize, cancellationToken);
        return Ok(new PagedResponse<NotificationResponse>(
            result.Items.Select(Map).ToArray(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<UnreadNotificationCountResponse>> GetUnreadCountAsync(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out Guid userId)) { return Unauthorized(); }
        int count = await notificationService.GetUnreadCountAsync(
            userId, cancellationToken);
        return Ok(new UnreadNotificationCountResponse(count));
    }

    [HttpPatch("{notificationId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out Guid userId)) { return Unauthorized(); }
        bool updated = await notificationService.MarkAsReadAsync(
            userId, notificationId, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [HttpPatch("read-all")]
    public async Task<ActionResult<MarkAllNotificationsReadResponse>> MarkAllAsReadAsync(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out Guid userId)) { return Unauthorized(); }
        int count = await notificationService.MarkAllAsReadAsync(
            userId, cancellationToken);
        return Ok(new MarkAllNotificationsReadResponse(count));
    }

    private bool TryGetUserId(out Guid userId)
    {
        return Guid.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out userId)
            && userId != Guid.Empty;
    }

    private static NotificationResponse Map(NotificationResult result)
    {
        return new NotificationResponse(
            result.Id,
            result.Type,
            result.Title,
            result.Body,
            result.RelatedEntityId,
            result.ActionUrl,
            result.CreatedAtUtc,
            result.ReadAtUtc);
    }
}
