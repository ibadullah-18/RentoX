using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Common;
using RentoX.Application.Messaging;
using RentoX.Contracts.Common;
using RentoX.Contracts.Messaging;

namespace RentoX.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/conversations")]
public sealed class ConversationsController(
    IConversationService conversationService)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<StartConversationResponse>(
        StatusCodes.Status201Created)]
    [ProducesResponseType<StartConversationResponse>(
        StatusCodes.Status200OK)]
    public async Task<ActionResult<StartConversationResponse>>
        StartAsync(
            StartConversationRequest request,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out Guid userId))
        {
            return Unauthorized();
        }

        StartConversationResult result =
            await conversationService.StartAsync(
                userId,
                request.ListingId,
                request.Body,
                cancellationToken);

        StartConversationResponse response = new(
            result.ConversationId,
            result.Created,
            MapMessage(result.Message));

        return result.Created
            ? Created(
                $"/api/conversations/{result.ConversationId}/messages",
                response)
            : Ok(response);
    }

    [HttpGet("unread-count")]
    [ProducesResponseType<UnreadMessageCountResponse>(
        StatusCodes.Status200OK)]
    public async Task<ActionResult<UnreadMessageCountResponse>>
        GetUnreadCountAsync(
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out Guid userId))
        {
            return Unauthorized();
        }

        int count =
            await conversationService.GetUnreadCountAsync(
                userId,
                cancellationToken);

        return Ok(
            new UnreadMessageCountResponse(count));
    }

    [HttpGet]
    public async Task<ActionResult<
        PagedResponse<ConversationSummaryResponse>>>
        GetMineAsync(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out Guid userId))
        {
            return Unauthorized();
        }

        PagedResult<ConversationSummaryResult> result =
            await conversationService.GetMineAsync(
                userId,
                page,
                pageSize,
                cancellationToken);

        ConversationSummaryResponse[] items =
            result.Items.Select(item =>
                new ConversationSummaryResponse(
                    item.Id,
                    item.ListingId,
                    item.ListingTitle,
                    item.OtherUserId,
                    item.LastMessage,
                    item.LastMessageAtUtc,
                    item.UnreadCount))
                .ToArray();

        return Ok(new PagedResponse<
            ConversationSummaryResponse>(
                items,
                result.Page,
                result.PageSize,
                result.TotalCount,
                result.TotalPages));
    }

    [HttpGet("{conversationId:guid}/messages")]
    public async Task<ActionResult<
        PagedResponse<MessageResponse>>>
        GetMessagesAsync(
            Guid conversationId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out Guid userId))
        {
            return Unauthorized();
        }

        PagedResult<MessageResult>? result =
            await conversationService.GetMessagesAsync(
                userId,
                conversationId,
                page,
                pageSize,
                cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(new PagedResponse<MessageResponse>(
            result.Items.Select(MapMessage).ToArray(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages));
    }

    [HttpPost("{conversationId:guid}/messages")]
    [ProducesResponseType<MessageResponse>(
        StatusCodes.Status201Created)]
    public async Task<ActionResult<MessageResponse>>
        SendAsync(
            Guid conversationId,
            SendMessageRequest request,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out Guid userId))
        {
            return Unauthorized();
        }

        MessageResult? result =
            await conversationService.SendAsync(
                userId,
                conversationId,
                request.Body,
                cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        MessageResponse response = MapMessage(result);

        return Created(
            $"/api/conversations/{conversationId}/messages",
            response);
    }

    [HttpPatch("{conversationId:guid}/read")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkReadAsync(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out Guid userId))
        {
            return Unauthorized();
        }

        bool found =
            await conversationService.MarkReadAsync(
                userId,
                conversationId,
                cancellationToken);

        return found ? NoContent() : NotFound();
    }

    private bool TryGetUserId(out Guid userId)
    {
        return Guid.TryParse(
            User.FindFirstValue(
                ClaimTypes.NameIdentifier),
            out userId);
    }

    private static MessageResponse MapMessage(
        MessageResult item)
    {
        return new MessageResponse(
            item.Id,
            item.ConversationId,
            item.SenderId,
            item.Body,
            item.SentAtUtc,
            item.ReadAtUtc)
        {
            Images = item.Images.Select(image => new MessageImageResponse(
                image.Id, image.Url, image.ContentType, image.SizeBytes, image.DisplayOrder))
                .ToArray()
        };
    }
}
