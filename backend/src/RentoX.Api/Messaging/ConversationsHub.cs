using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

using RentoX.Application.Messaging;

namespace RentoX.Api.Messaging;

[Authorize]
public sealed class ConversationsHub(IConversationService conversationService) : Hub
{
    public override Task OnConnectedAsync()
    {
        if (!Guid.TryParse(
                Context.UserIdentifier,
                out _))
        {
            Context.Abort();
            return Task.CompletedTask;
        }

        return base.OnConnectedAsync();
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
        if (!Guid.TryParse(Context.UserIdentifier, out Guid userId) ||
            userId == Guid.Empty)
        {
            throw new HubException("Authentication is required.");
        }

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
}

public sealed record TypingChangedEvent(
    Guid ConversationId,
    Guid UserId);
