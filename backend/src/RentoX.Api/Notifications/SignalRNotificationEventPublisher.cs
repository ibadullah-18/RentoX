using Microsoft.AspNetCore.SignalR;
using RentoX.Application.Notifications;

namespace RentoX.Api.Notifications;

public sealed class SignalRNotificationEventPublisher(
    IHubContext<NotificationsHub> hubContext,
    ILogger<SignalRNotificationEventPublisher> logger)
    : INotificationEventPublisher
{
    private static readonly Action<ILogger, Guid, Exception?>
        DeliveryFailed = LoggerMessage.Define<Guid>(
            LogLevel.Warning,
            new EventId(1301, "NotificationDeliveryFailed"),
            "Live notification delivery failed for user {UserId}");

    public async Task NotificationCreatedAsync(
        NotificationResult notification,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await hubContext.Clients
                .User(notification.UserId.ToString("D"))
                .SendAsync(
                    "NotificationCreated",
                    notification,
                    cancellationToken);
        }
        catch (Exception exception)
        {
            DeliveryFailed(logger, notification.UserId, exception);
        }
    }
}
