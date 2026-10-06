using Microsoft.Extensions.Options;

namespace RentoX.Worker.PushNotifications;

public sealed partial class PushNotificationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<PushNotificationOptions> options,
    ILogger<PushNotificationWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        PushNotificationOptions settings =
            options.Value;

        ValidateSettings(settings);

        if (!settings.Enabled)
        {
            LogPushWorkerDisabled(logger);
            return;
        }

        LogPushWorkerStarted(
            logger,
            settings.IntervalSeconds,
            settings.BatchSize);

        TimeSpan interval =
            TimeSpan.FromSeconds(
                settings.IntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope =
                    scopeFactory.CreateScope();

                PushNotificationProcessor processor =
                    scope.ServiceProvider
                        .GetRequiredService<
                            PushNotificationProcessor>();

                PushProcessingResult result =
                    await processor.RunAsync(
                        stoppingToken);

                if (result.ProcessedCount > 0 ||
                    result.RecoveredCount > 0)
                {
                    LogPushBatchCompleted(
                        logger,
                        result.RecoveredCount,
                        result.ProcessedCount,
                        result.SentCount,
                        result.FailedCount,
                        result.DeadLetterCount,
                        result.DeactivatedDeviceCount);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                LogPushWorkerFailure(
                    logger,
                    exception);
            }

            await Task.Delay(
                interval,
                stoppingToken);
        }
    }

    private static void ValidateSettings(
        PushNotificationOptions settings)
    {
        if (settings.IntervalSeconds <= 0 ||
            settings.IntervalSeconds > 3600)
        {
            throw new InvalidOperationException(
                "Push interval must be between 1 and 3600 seconds.");
        }

        if (settings.BatchSize <= 0 ||
            settings.BatchSize > 500)
        {
            throw new InvalidOperationException(
                "Push batch size must be between 1 and 500.");
        }

        if (settings.MaximumAttempts <= 0 ||
            settings.MaximumAttempts > 20)
        {
            throw new InvalidOperationException(
                "Maximum push attempts must be between 1 and 20.");
        }

        if (settings.ProcessingTimeoutMinutes <= 0 ||
            settings.ProcessingTimeoutMinutes > 60)
        {
            throw new InvalidOperationException(
                "Push processing timeout must be between 1 and 60 minutes.");
        }
    }

    [LoggerMessage(
        EventId = 4101,
        Level = LogLevel.Information,
        Message =
            "Push notification worker is disabled")]
    private static partial void LogPushWorkerDisabled(
        ILogger logger);

    [LoggerMessage(
        EventId = 4102,
        Level = LogLevel.Information,
        Message =
            "Push notification worker started. Interval: {IntervalSeconds} seconds, batch size: {BatchSize}")]
    private static partial void LogPushWorkerStarted(
        ILogger logger,
        int intervalSeconds,
        int batchSize);

    [LoggerMessage(
        EventId = 4103,
        Level = LogLevel.Information,
        Message =
            "Push batch completed. Recovered: {RecoveredCount}, processed: {ProcessedCount}, sent: {SentCount}, failed: {FailedCount}, dead-letter: {DeadLetterCount}, deactivated devices: {DeactivatedDeviceCount}")]
    private static partial void LogPushBatchCompleted(
        ILogger logger,
        int recoveredCount,
        int processedCount,
        int sentCount,
        int failedCount,
        int deadLetterCount,
        int deactivatedDeviceCount);

    [LoggerMessage(
        EventId = 4104,
        Level = LogLevel.Error,
        Message =
            "Push notification worker failed")]
    private static partial void LogPushWorkerFailure(
        ILogger logger,
        Exception exception);
}