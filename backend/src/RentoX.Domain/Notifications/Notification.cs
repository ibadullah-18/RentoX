using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Notifications;

public enum NotificationType
{
    ListingApproved = 1,
    ListingRejected = 2,
    ListingPaymentRequired = 3,
    StoreApproved = 4,
    StoreRejected = 5,
    ConversationReportUpdated = 6,
    SupportReply = 7,
    SupportStatusChanged = 8
}

public sealed class Notification : Entity
{
    private Notification() { }

    private Notification(
        Guid id,
        Guid userId,
        NotificationType type,
        string title,
        string body,
        Guid? relatedEntityId,
        string? actionUrl,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        UserId = userId;
        Type = type;
        Title = NormalizeRequired(title, 160, "Notification title");
        Body = NormalizeRequired(body, 1000, "Notification body");
        RelatedEntityId = relatedEntityId;
        ActionUrl = NormalizeOptional(actionUrl, 500, "Notification action URL");
        RequireUtc(createdAtUtc, "Notification creation time");
        CreatedAtUtc = createdAtUtc;
    }

    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public Guid? RelatedEntityId { get; private set; }
    public string? ActionUrl { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ReadAtUtc { get; private set; }

    public static Notification Create(
        Guid userId,
        NotificationType type,
        string title,
        string body,
        Guid? relatedEntityId,
        string? actionUrl,
        DateTimeOffset createdAtUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("Notification user id is required.");
        }

        if (!Enum.IsDefined(type))
        {
            throw new DomainException("Notification type is invalid.");
        }

        return new Notification(
            Guid.NewGuid(), userId, type, title, body,
            relatedEntityId, actionUrl, createdAtUtc);
    }

    public void MarkAsRead(DateTimeOffset readAtUtc)
    {
        RequireUtc(readAtUtc, "Notification read time");
        ReadAtUtc ??= readAtUtc;
    }

    private static string NormalizeRequired(
        string value,
        int maximumLength,
        string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{field} is required.");
        }

        string normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new DomainException(
                $"{field} cannot exceed {maximumLength} characters.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(
        string? value,
        int maximumLength,
        string field)
    {
        if (string.IsNullOrWhiteSpace(value)) { return null; }
        string normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new DomainException(
                $"{field} cannot exceed {maximumLength} characters.");
        }

        return normalized;
    }

    private static void RequireUtc(DateTimeOffset value, string field)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException($"{field} must be UTC.");
        }
    }
}
