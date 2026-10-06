using RentoX.Domain.Notifications;

namespace RentoX.Domain.UnitTests.Notifications;

public sealed class PushNotificationDeliveryTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(
            2026,
            10,
            3,
            12,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public void CreateShouldCreatePendingDelivery()
    {
        PushNotificationDelivery delivery =
            PushNotificationDelivery.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                CreatedAtUtc);

        Assert.Equal(
            PushDeliveryStatus.Pending,
            delivery.Status);

        Assert.Equal(0, delivery.AttemptCount);
        Assert.Equal(
            CreatedAtUtc,
            delivery.NextAttemptAtUtc);

        Assert.Null(delivery.SentAtUtc);
        Assert.Null(delivery.LastError);
    }

    [Fact]
    public void SuccessfulAttemptShouldMarkDeliveryAsSent()
    {
        PushNotificationDelivery delivery =
            CreateDelivery();

        DateTimeOffset startedAtUtc =
            CreatedAtUtc.AddMinutes(1);

        delivery.BeginAttempt(
            startedAtUtc,
            startedAtUtc.AddMinutes(5));

        delivery.MarkAsSent(
            startedAtUtc.AddSeconds(2));

        Assert.Equal(
            PushDeliveryStatus.Sent,
            delivery.Status);

        Assert.Equal(1, delivery.AttemptCount);
        Assert.NotNull(delivery.SentAtUtc);
        Assert.Null(delivery.LastError);
    }

    [Fact]
    public void FailedAttemptShouldScheduleRetry()
    {
        PushNotificationDelivery delivery =
            CreateDelivery();

        DateTimeOffset startedAtUtc =
            CreatedAtUtc.AddMinutes(1);

        DateTimeOffset retryAtUtc =
            startedAtUtc.AddMinutes(5);

        delivery.BeginAttempt(
            startedAtUtc,
            startedAtUtc.AddMinutes(2));

        delivery.MarkAsFailed(
            "Temporary Firebase error.",
            retryAtUtc,
            5);

        Assert.Equal(
            PushDeliveryStatus.Failed,
            delivery.Status);

        Assert.Equal(
            retryAtUtc,
            delivery.NextAttemptAtUtc);

        Assert.Equal(
            "Temporary Firebase error.",
            delivery.LastError);
    }

    [Fact]
    public void LastAllowedAttemptShouldBecomeDeadLetter()
    {
        PushNotificationDelivery delivery =
            CreateDelivery();

        DateTimeOffset currentTime =
            CreatedAtUtc;

        for (int attempt = 1;
             attempt <= 5;
             attempt++)
        {
            currentTime =
                currentTime.AddMinutes(1);

            delivery.BeginAttempt(
                currentTime,
                currentTime.AddMinutes(1));

            delivery.MarkAsFailed(
                "Firebase error.",
                currentTime,
                5);
        }

        Assert.Equal(
            PushDeliveryStatus.DeadLetter,
            delivery.Status);

        Assert.Equal(5, delivery.AttemptCount);
    }

    [Fact]
    public void TimedOutAttemptShouldBeRecovered()
    {
        PushNotificationDelivery delivery =
            CreateDelivery();

        DateTimeOffset startedAtUtc =
            CreatedAtUtc.AddMinutes(1);

        DateTimeOffset deadlineUtc =
            startedAtUtc.AddMinutes(5);

        delivery.BeginAttempt(
            startedAtUtc,
            deadlineUtc);

        delivery.RecoverTimedOutAttempt(
            deadlineUtc);

        Assert.Equal(
            PushDeliveryStatus.Failed,
            delivery.Status);

        Assert.Equal(
            deadlineUtc,
            delivery.NextAttemptAtUtc);

        Assert.NotNull(delivery.LastError);
    }

    private static PushNotificationDelivery
        CreateDelivery()
    {
        return PushNotificationDelivery.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CreatedAtUtc);
    }
}