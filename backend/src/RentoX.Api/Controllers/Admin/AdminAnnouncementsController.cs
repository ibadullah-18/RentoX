using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Admin;
using RentoX.Application.Authorization;
using RentoX.Contracts.Common;

namespace RentoX.Api.Controllers.Admin;

/// <summary>Sending a message to one person or to everyone. Needs the users section.</summary>
[ApiController]
[Route("api/admin/announcements")]
[Authorize(Policy = PolicyNames.AdminAccess)]
[Authorize(Policy = AdminSections.UsersPolicy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminAnnouncementsController(IAnnouncementService announcementService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<AnnouncementHistoryItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<AnnouncementHistoryItem>>> HistoryAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await announcementService.GetHistoryAsync(page, pageSize, cancellationToken);
        return Ok(new PagedResponse<AnnouncementHistoryItem>(
            result.Items, result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }

    [HttpPost]
    [ProducesResponseType<AnnouncementResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AnnouncementResult>> SendAsync(
        AnnouncementRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await announcementService.SendAsync(
            request.UserId, request.Audience ?? AnnouncementAudience.Everyone, request.Title, request.Body, request.ActionUrl, cancellationToken));
    }
}

/// <param name="UserId">Null: the audience.</param>
/// <param name="Audience">Everyone (default), store owners or sellers; ignored when UserId is set.</param>
public sealed record AnnouncementRequest(Guid? UserId, AnnouncementAudience? Audience, string Title, string Body, string? ActionUrl);
