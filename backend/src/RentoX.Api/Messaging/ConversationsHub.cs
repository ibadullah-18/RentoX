using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RentoX.Application.Messaging;

namespace RentoX.Api.Messaging;

[Authorize]
public sealed class ConversationsHub(
    IConversationService conversationService,
    IUserPresenceStore presence)
    : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (!Guid.TryParse(
                Context.UserIdentifier,
                out Guid userId) ||
            userId == Guid.Empty)
        {
            Context.Abort();
            return;
        }

        await presence.RefreshAsync(
            userId,
            Context.ConnectionId,
            Context.ConnectionAborted);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(
        Exception? exception)
    {
        try
        {
            if (Guid.TryParse(
                    Context.UserIdentifier,
                    out Guid userId) &&
                userId != Guid.Empty)
            {
                await presence.RemoveAsync(
                    userId,
                    Context.ConnectionId,
                    CancellationToken.None);
            }
        }
        finally
        {
            await base.OnDisconnectedAsync(exception);
        }
    }

    public Task<bool> Heartbeat()
    {
        return presence.RefreshAsync(
            RequireUserId(),
            Context.ConnectionId,
            Context.ConnectionAborted);
    }

    public Task StartTyping(Guid conversationId)
    {
        return PublishTypingAsync(conversationId, isTyping: true);
    }

    public Task StopTyping(Guid conversationId)
    {
        return PublishTypingAsync(conversationId, isTyping: false);
    }

    private async Task PublishTypingAsync(
        Guid conversationId,
        bool isTyping)
    {
        Guid userId = RequireUserId();

        Guid? otherUserId =
            await conversationService.GetOtherParticipantIdAsync(
                userId,
                conversationId,
                Context.ConnectionAborted);

        if (!otherUserId.HasValue)
        {
            throw new HubException("Conversation was not found.");
        }

        await Clients.User(otherUserId.Value.ToString())
            .SendAsync(
                isTyping ? "TypingStarted" : "TypingStopped",
                new TypingChangedEvent(conversationId, userId),
                Context.ConnectionAborted);
    }

    private Guid RequireUserId()
    {
        if (!Guid.TryParse(
                Context.UserIdentifier,
                out Guid userId) ||
            userId == Guid.Empty)
        {
            throw new HubException("Authentication is required.");
        }

        return userId;
    }
}

public sealed record TypingChangedEvent(
    Guid ConversationId,
    Guid UserId);
