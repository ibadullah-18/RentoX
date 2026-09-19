using RentoX.Application.Common;

namespace RentoX.Application.Messaging;

public interface IConversationService
{
    Task<int> GetUnreadCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<StartConversationResult> StartAsync(
        Guid buyerId,
        Guid listingId,
        string body,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ConversationSummaryResult>> GetMineAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PagedResult<MessageResult>?> GetMessagesAsync(
        Guid userId,
        Guid conversationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<MessageResult?> SendAsync(
        Guid userId,
        Guid conversationId,
        string body,
        CancellationToken cancellationToken = default);

    Task<bool> MarkReadAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken = default);

    Task<Guid?> GetOtherParticipantIdAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken = default);
}