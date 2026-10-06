namespace RentoX.Application.Messaging;

public sealed record ConversationBlockStatusResult(
    Guid ConversationId,
    bool IsBlockedByMe,
    bool CanSendMessages);

public interface IConversationBlockService
{
    Task<ConversationBlockStatusResult?> GetStatusAsync(
        Guid userId, Guid conversationId, CancellationToken cancellationToken = default);

    Task<bool> BlockAsync(
        Guid userId, Guid conversationId, CancellationToken cancellationToken = default);

    Task<bool> UnblockAsync(
        Guid userId, Guid conversationId, CancellationToken cancellationToken = default);
}
