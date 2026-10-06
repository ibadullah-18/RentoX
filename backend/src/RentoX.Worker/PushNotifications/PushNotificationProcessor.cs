using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RentoX.Application.Abstractions.Time;
using RentoX.Domain.Notifications;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Worker.PushNotifications;

public sealed record PushProcessingResult(
    int RecoveredCount,
    int ProcessedCount,
    int SentCount,
    int FailedCount,
    int DeadLetterCount,
    int DeactivatedDeviceCount);

public sealed class PushNotificationProcessor(
    RentoXDbContext dbContext,
    IPushNotificationSender sender,
    IClock clock,
    IOptions<PushNotificationOptions> options)
{
    public async Task<PushProcessingResult> RunAsync(
        CancellationToken cancellationToken = default)
    {
        PushNotificationOptions settings =
            options.Value;

        DateTimeOffset nowUtc =
            clock.UtcNow;

        int recoveredCount =
            await RecoverTimedOutDeliveriesAsync(
                nowUtc,
                cancellationToken);

        List<Guid> deliveryIds =
            await dbContext
                .Set<PushNotificationDelivery>()
                .AsNoTracking()
                .Where(delivery =>
                    (delivery.Status ==
                        PushDeliveryStatus.Pending ||
                     delivery.Status ==
                        PushDeliveryStatus.Failed) &&
                    delivery.NextAttemptAtUtc <= nowUtc)
                .OrderBy(delivery =>
                    delivery.NextAttemptAtUtc)
                .ThenBy(delivery =>
                    delivery.CreatedAtUtc)
                .Select(delivery => delivery.Id)
                .Take(settings.BatchSize)
                .ToListAsync(cancellationToken);

        int sentCount = 0;
        int failedCount = 0;
        int deadLetterCount = 0;
        int deactivatedDeviceCount = 0;

        foreach (Guid deliveryId in deliveryIds)
        {
            DeliveryProcessingOutcome outcome =
                await ProcessDeliveryAsync(
                    deliveryId,
                    settings,
                    cancellationToken);

            sentCount += outcome.SentCount;
            failedCount += outcome.FailedCount;
            deadLetterCount +=
                outcome.DeadLetterCount;
            deactivatedDeviceCount +=
                outcome.DeactivatedDeviceCount;
        }

        return new PushProcessingResult(
            recoveredCount,
            deliveryIds.Count,
            sentCount,
            failedCount,
            deadLetterCount,
            deactivatedDeviceCount);
    }

    private async Task<int>
        RecoverTimedOutDeliveriesAsync(
            DateTimeOffset nowUtc,
            CancellationToken cancellationToken)
    {
        List<PushNotificationDelivery> deliveries =
            await dbContext
                .Set<PushNotificationDelivery>()
                .Where(delivery =>
                    delivery.Status ==
                        PushDeliveryStatus.Processing &&
                    delivery.NextAttemptAtUtc <= nowUtc)
                .ToListAsync(cancellationToken);

        foreach (PushNotificationDelivery delivery
                 in deliveries)
        {
            delivery.RecoverTimedOutAttempt(nowUtc);
        }

        if (deliveries.Count > 0)
        {
            await dbContext.SaveChangesAsync(
                cancellationToken);
        }

        return deliveries.Count;
    }

    private async Task<DeliveryProcessingOutcome>
        ProcessDeliveryAsync(
            Guid deliveryId,
            PushNotificationOptions settings,
            CancellationToken cancellationToken)
    {
        PushNotificationDelivery? delivery =
            await dbContext
                .Set<PushNotificationDelivery>()
                .SingleOrDefaultAsync(
                    item => item.Id == deliveryId,
                    cancellationToken);

        if (delivery is null)
        {
            return DeliveryProcessingOutcome.None;
        }

        DateTimeOffset startedAtUtc =
            clock.UtcNow;

        delivery.BeginAttempt(
            startedAtUtc,
            startedAtUtc.AddMinutes(
                settings.ProcessingTimeoutMinutes));

        await dbContext.SaveChangesAsync(
            cancellationToken);

        try
        {
            return await SendDeliveryAsync(
                delivery,
                settings,
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            DateTimeOffset failedAtUtc =
                clock.UtcNow;

            delivery.MarkAsFailed(
                exception.Message,
                CalculateNextAttempt(
                    failedAtUtc,
                    delivery.AttemptCount),
                settings.MaximumAttempts);

            await dbContext.SaveChangesAsync(
                cancellationToken);

            return delivery.Status ==
                   PushDeliveryStatus.DeadLetter
                ? DeliveryProcessingOutcome.DeadLetter
                : DeliveryProcessingOutcome.Failed;
        }
    }

    private async Task<DeliveryProcessingOutcome>
        SendDeliveryAsync(
            PushNotificationDelivery delivery,
            PushNotificationOptions settings,
            CancellationToken cancellationToken)
    {
        Notification? notification =
            await dbContext
                .Set<Notification>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.Id ==
                        delivery.NotificationId,
                    cancellationToken);

        DateTimeOffset nowUtc =
            clock.UtcNow;

        if (notification is null)
        {
            delivery.MarkAsDeadLetter(
                "The notification no longer exists.",
                nowUtc);

            await dbContext.SaveChangesAsync(
                cancellationToken);

            return DeliveryProcessingOutcome.DeadLetter;
        }

        List<PushDevice> devices =
            await dbContext
                .Set<PushDevice>()
                .Where(device =>
                    device.UserId == delivery.UserId &&
                    device.IsActive)
                .OrderByDescending(device =>
                    device.LastSeenAtUtc)
                .Take(500)
                .ToListAsync(cancellationToken);

        if (devices.Count == 0)
        {
            delivery.MarkAsDeadLetter(
                "The user has no active push device.",
                nowUtc);

            await dbContext.SaveChangesAsync(
                cancellationToken);

            return DeliveryProcessingOutcome.DeadLetter;
        }

        string[] tokens =
            devices
                .Select(device => device.Token)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

        PushSendResult result =
            await sender.SendAsync(
                new PushSendRequest(
                    notification.Title,
                    notification.Body,
                    notification.ActionUrl,
                    notification.Id,
                    notification.RelatedEntityId,
                    tokens),
                cancellationToken);

        HashSet<string> invalidTokens =
            new(
                result.InvalidTokens,
                StringComparer.Ordinal);

        int deactivatedDeviceCount = 0;

        foreach (PushDevice device in devices)
        {
            if (!invalidTokens.Contains(
                    device.Token))
            {
                continue;
            }

            device.Deactivate(clock.UtcNow);
            deactivatedDeviceCount++;
        }

        DateTimeOffset completedAtUtc =
            clock.UtcNow;

        if (result.SuccessCount > 0)
        {
            delivery.MarkAsSent(
                completedAtUtc);

            await dbContext.SaveChangesAsync(
                cancellationToken);

            return new DeliveryProcessingOutcome(
                1,
                0,
                0,
                deactivatedDeviceCount);
        }

        if (invalidTokens.Count == tokens.Length)
        {
            delivery.MarkAsDeadLetter(
                result.Error ??
                "All Firebase push tokens are invalid.",
                completedAtUtc);

            await dbContext.SaveChangesAsync(
                cancellationToken);

            return new DeliveryProcessingOutcome(
                0,
                0,
                1,
                deactivatedDeviceCount);
        }

        delivery.MarkAsFailed(
            result.Error ??
            "Firebase could not deliver the push notification.",
            CalculateNextAttempt(
                completedAtUtc,
                delivery.AttemptCount),
            settings.MaximumAttempts);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return new DeliveryProcessingOutcome(
            0,
            delivery.Status ==
                PushDeliveryStatus.Failed
                ? 1
                : 0,
            delivery.Status ==
                PushDeliveryStatus.DeadLetter
                ? 1
                : 0,
            deactivatedDeviceCount);
    }

    private static DateTimeOffset
        CalculateNextAttempt(
            DateTimeOffset failedAtUtc,
            int attemptCount)
    {
        return attemptCount switch
        {
            <= 1 => failedAtUtc.AddMinutes(1),
            2 => failedAtUtc.AddMinutes(5),
            3 => failedAtUtc.AddMinutes(15),
            _ => failedAtUtc.AddHours(1)
        };
    }

    private sealed record DeliveryProcessingOutcome(
        int SentCount,
        int FailedCount,
        int DeadLetterCount,
        int DeactivatedDeviceCount)
    {
        public static DeliveryProcessingOutcome None { get; } =
            new(0, 0, 0, 0);

        public static DeliveryProcessingOutcome Failed { get; } =
            new(0, 1, 0, 0);

        public static DeliveryProcessingOutcome DeadLetter { get; } =
            new(0, 0, 1, 0);
    }
}