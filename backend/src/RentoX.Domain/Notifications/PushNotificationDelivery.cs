using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Notifications;

public enum PushDeliveryStatus
{
    Pending = 1,
    Processing = 2,
    Sent = 3,
    Failed = 4,
    DeadLetter = 5
}

public sealed class PushNotificationDelivery : Entity
{
    private const int MaximumErrorLength = 2000;

    private PushNotificationDelivery()
    {
    }

    private PushNotificationDelivery(
        Guid id,
        Guid notificationId,
        Guid userId,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        if (notificationId == Guid.Empty ||
            userId == Guid.Empty)
        {
            throw new DomainException(
                "Push notification and user ids are required.");
        }

        RequireUtc(
            createdAtUtc,
            "Push delivery creation time");

        NotificationId = notificationId;
        UserId = userId;
        Status = PushDeliveryStatus.Pending;
        CreatedAtUtc = createdAtUtc;
        NextAttemptAtUtc = createdAtUtc;
    }

    public Guid NotificationId { get; private set; }

    public Guid UserId { get; private set; }

    public PushDeliveryStatus Status { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset NextAttemptAtUtc { get; private set; }

    public DateTimeOffset? SentAtUtc { get; private set; }

    public string? LastError { get; private set; }

    public static PushNotificationDelivery Create(
        Guid notificationId,
        Guid userId,
        DateTimeOffset createdAtUtc)
    {
        return new PushNotificationDelivery(
            Guid.NewGuid(),
            notificationId,
            userId,
            createdAtUtc);
    }

    public void BeginAttempt(
        DateTimeOffset startedAtUtc,
        DateTimeOffset processingDeadlineUtc)
    {
        RequireUtc(
            startedAtUtc,
            "Push attempt start time");

        RequireUtc(
            processingDeadlineUtc,
            "Push processing deadline");

        if (Status is not
            (PushDeliveryStatus.Pending or
             PushDeliveryStatus.Failed))
        {
            throw new DomainException(
                "Push delivery cannot be processed in its current status.");
        }

        if (NextAttemptAtUtc > startedAtUtc)
        {
            throw new DomainException(
                "Push delivery is not ready for another attempt.");
        }

        if (processingDeadlineUtc <= startedAtUtc)
        {
            throw new DomainException(
                "Push processing deadline must be in the future.");
        }

        Status = PushDeliveryStatus.Processing;
        AttemptCount++;
        NextAttemptAtUtc = processingDeadlineUtc;
        LastError = null;
    }

    public void MarkAsSent(
        DateTimeOffset sentAtUtc)
    {
        RequireUtc(
            sentAtUtc,
            "Push sent time");

        EnsureProcessing();

        Status = PushDeliveryStatus.Sent;
        SentAtUtc = sentAtUtc;
        NextAttemptAtUtc = sentAtUtc;
        LastError = null;
    }

    public void MarkAsFailed(
        string error,
        DateTimeOffset nextAttemptAtUtc,
        int maximumAttempts)
    {
        RequireUtc(
            nextAttemptAtUtc,
            "Push next-attempt time");

        EnsureProcessing();

        if (maximumAttempts <= 0)
        {
            throw new DomainException(
                "Maximum push attempts must be positive.");
        }

        LastError = NormalizeError(error);
        NextAttemptAtUtc = nextAttemptAtUtc;

        Status =
            AttemptCount >= maximumAttempts
                ? PushDeliveryStatus.DeadLetter
                : PushDeliveryStatus.Failed;
    }

    public void MarkAsDeadLetter(
        string error,
        DateTimeOffset occurredAtUtc)
    {
        RequireUtc(
            occurredAtUtc,
            "Push dead-letter time");

        if (Status == PushDeliveryStatus.Sent)
        {
            throw new DomainException(
                "A sent push delivery cannot become dead-letter.");
        }

        Status = PushDeliveryStatus.DeadLetter;
        LastError = NormalizeError(error);
        NextAttemptAtUtc = occurredAtUtc;
    }

    public void RecoverTimedOutAttempt(
        DateTimeOffset recoveredAtUtc)
    {
        RequireUtc(
            recoveredAtUtc,
            "Push recovery time");

        if (Status != PushDeliveryStatus.Processing)
        {
            throw new DomainException(
                "Only a processing push delivery can be recovered.");
        }

        if (NextAttemptAtUtc > recoveredAtUtc)
        {
            throw new DomainException(
                "Push delivery processing has not timed out.");
        }

        Status = PushDeliveryStatus.Failed;
        LastError =
            "The previous push processing attempt timed out.";
        NextAttemptAtUtc = recoveredAtUtc;
    }

    private void EnsureProcessing()
    {
        if (Status != PushDeliveryStatus.Processing)
        {
            throw new DomainException(
                "Push delivery is not being processed.");
        }
    }

    private static string NormalizeError(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new DomainException(
                "Push delivery error is required.");
        }

        string normalized = error.Trim();

        return normalized.Length <= MaximumErrorLength
            ? normalized
            : normalized[..MaximumErrorLength];
    }

    private static void RequireUtc(
        DateTimeOffset value,
        string field)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(
                $"{field} must be UTC.");
        }
    }
}