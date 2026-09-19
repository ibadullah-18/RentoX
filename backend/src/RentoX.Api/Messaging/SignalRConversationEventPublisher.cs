using Microsoft.AspNetCore.SignalR;
using RentoX.Application.Messaging;

namespace RentoX.Api.Messaging;

public sealed class SignalRConversationEventPublisher(
    IHubContext<ConversationsHub> hubContext,
    ILogger<SignalRConversationEventPublisher> logger)
    : IConversationEventPublisher
{
    private static readonly Action<ILogger, Guid, Exception?>
        MessageDeliveryFailed =
            LoggerMessage.Define<Guid>(
                LogLevel.Warning,
                new EventId(1201, "MessageDeliveryFailed"),
                "Live message delivery failed for conversation {ConversationId}");

    private static readonly Action<ILogger, Guid, Exception?>
        ReadReceiptDeliveryFailed =
            LoggerMessage.Define<Guid>(
                LogLevel.Warning,
                new EventId(1202, "ReadReceiptDeliveryFailed"),
                "Live read receipt delivery failed for conversation {ConversationId}");
    public async Task MessageCreatedAsync(
        Guid buyerId,
        Guid sellerId,
        MessageResult message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await hubContext.Clients.Users(
                    [
                        buyerId.ToString("D"),
                        sellerId.ToString("D")
                    ])
                .SendAsync(
                    "MessageCreated",
                    message,
                    cancellationToken);
        }
        catch (Exception exception)
        {
            MessageDeliveryFailed(logger, message.ConversationId, exception);
        }
    }

    public async Task MessagesReadAsync(
        Guid buyerId,
        Guid sellerId,
        Guid conversationId,
        Guid readByUserId,
        DateTimeOffset readAtUtc,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await hubContext.Clients.Users(
                    [
                        buyerId.ToString("D"),
                        sellerId.ToString("D")
                    ])
                .SendAsync(
                    "MessagesRead",
                    new
                    {
                        ConversationId = conversationId,
                        ReadByUserId = readByUserId,
                        ReadAtUtc = readAtUtc
                    },
                    cancellationToken);
        }
        catch (Exception exception)
        {
            ReadReceiptDeliveryFailed(logger, conversationId, exception);
        }
    }
}