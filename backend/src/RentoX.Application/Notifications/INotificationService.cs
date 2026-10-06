using RentoX.Application.Common;

namespace RentoX.Application.Notifications;

public sealed record CreateNotificationCommand(
    Guid UserId,
    int Type,
    string Title,
    string Body,
    Guid? RelatedEntityId,
    string? ActionUrl);

public sealed record NotificationResult(
    Guid Id,
    Guid UserId,
    int Type,
    string Title,
    string Body,
    Guid? RelatedEntityId,
    string? ActionUrl,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReadAtUtc);

public interface INotificationService
{
    Task<NotificationResult> CreateAsync(
        CreateNotificationCommand command,
        CancellationToken cancellationToken = default);

    Task<PagedResult<NotificationResult>> GetAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> MarkAsReadAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken = default);

    Task<int> MarkAllAsReadAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public interface INotificationEventPublisher
{
    Task NotificationCreatedAsync(
        NotificationResult notification,
        CancellationToken cancellationToken = default);
}
