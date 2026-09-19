namespace RentoX.Application.Messaging;

public interface IConversationEventPublisher
{
    Task MessageCreatedAsync(
        Guid buyerId,
        Guid sellerId,
        MessageResult message,
        CancellationToken cancellationToken = default);

    Task MessagesReadAsync(
        Guid buyerId,
        Guid sellerId,
        Guid conversationId,
        Guid readByUserId,
        DateTimeOffset readAtUtc,
        CancellationToken cancellationToken = default);
}