using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Messaging;
using RentoX.Contracts.Messaging;

namespace RentoX.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/conversations/{conversationId:guid}/block")]
public sealed class ConversationBlocksController(IConversationBlockService blockService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ConversationBlockStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConversationBlockStatusResponse>> GetStatusAsync(
        Guid conversationId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out Guid userId)) { return Unauthorized(); }
        ConversationBlockStatusResult? result = await blockService.GetStatusAsync(
            userId, conversationId, cancellationToken);
        if (result is null) { return NotFound(); }
        return Ok(new ConversationBlockStatusResponse(
            result.ConversationId, result.IsBlockedByMe, result.CanSendMessages));
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BlockAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out Guid userId)) { return Unauthorized(); }
        bool found = await blockService.BlockAsync(userId, conversationId, cancellationToken);
        return found ? NoContent() : NotFound();
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnblockAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out Guid userId)) { return Unauthorized(); }
        bool found = await blockService.UnblockAsync(userId, conversationId, cancellationToken);
        return found ? NoContent() : NotFound();
    }

    private bool TryGetUserId(out Guid userId)
    {
        return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId)
            && userId != Guid.Empty;
    }
}
