namespace RentoX.Contracts.Notifications;

public sealed record NotificationResponse(
    Guid Id,
    int Type,
    string Title,
    string Body,
    Guid? RelatedEntityId,
    string? ActionUrl,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReadAtUtc);

public sealed record UnreadNotificationCountResponse(int Count);

public sealed record MarkAllNotificationsReadResponse(int UpdatedCount);
