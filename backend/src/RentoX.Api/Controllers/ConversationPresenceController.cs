using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Messaging;
using RentoX.Contracts.Messaging;

namespace RentoX.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/conversations/{conversationId:guid}/presence")]
public sealed class ConversationPresenceController(
    IConversationService conversationService,
    IUserPresenceStore presence)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ConversationPresenceResponse>>
        GetAsync(
            Guid conversationId,
            CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out Guid userId) ||
            userId == Guid.Empty)
        {
            return Unauthorized();
        }

        Guid? otherUserId =
            await conversationService.GetOtherParticipantIdAsync(
                userId,
                conversationId,
                cancellationToken);

        if (!otherUserId.HasValue)
        {
            return NotFound();
        }

        bool? isOnline = await presence.IsOnlineAsync(
            otherUserId.Value,
            cancellationToken);

        if (!isOnline.HasValue)
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Presence is temporarily unavailable.");
        }

        return Ok(new ConversationPresenceResponse(
            otherUserId.Value,
            isOnline.Value));
    }
}
