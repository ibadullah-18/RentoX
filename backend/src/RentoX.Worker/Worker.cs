using Microsoft.Extensions.Options;
using RentoX.Application.Listings;

namespace RentoX.Worker;

public sealed partial class Worker(
    IServiceScopeFactory scopeFactory,
    IOptions<ListingMaintenanceOptions> options,
    ILogger<Worker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        ListingMaintenanceOptions settings =
            options.Value;

        ValidateSettings(settings);

        TimeSpan interval =
            TimeSpan.FromMinutes(
                settings.IntervalMinutes);

        TimeSpan deletionRetention =
            TimeSpan.FromDays(
                settings.DeletedImageRetentionDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope =
                    scopeFactory.CreateScope();

                IListingMaintenanceService service =
                    scope.ServiceProvider
                        .GetRequiredService<
                            IListingMaintenanceService>();

                ListingMaintenanceResult result =
                    await service.RunAsync(
                        deletionRetention,
                        settings.CleanupBatchSize,
                        stoppingToken);

                if (settings.ExpiryReminderDays > 0)
                {
                    int reminded = await scope.ServiceProvider
                        .GetRequiredService<IListingExpiryReminderService>()
                        .RemindAsync(
                            TimeSpan.FromDays(settings.ExpiryReminderDays),
                            settings.CleanupBatchSize,
                            stoppingToken);

                    LogRemindersWritten(logger, reminded);
                }

                LogMaintenanceCompleted(
                    logger,
                    result.ExpiredListingCount,
                    result.PurgedImageCount,
                    result.FailedImageCount);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                LogMaintenanceFailure(
                    logger,
                    exception);
            }

            await Task.Delay(
                interval,
                stoppingToken);
        }
    }

    private static void ValidateSettings(
        ListingMaintenanceOptions settings)
    {
        if (settings.IntervalMinutes <= 0)
        {
            throw new InvalidOperationException(
                "Worker interval must be positive.");
        }

        if (settings.DeletedImageRetentionDays < 0)
        {
            throw new InvalidOperationException(
                "Deleted image retention cannot be negative.");
        }

        if (settings.ExpiryReminderDays < 0 || settings.ExpiryReminderDays > 30)
        {
            throw new InvalidOperationException(
                "Expiry reminder days must be between 0 and 30.");
        }

        if (settings.CleanupBatchSize <= 0 ||
            settings.CleanupBatchSize > 1000)
        {
            throw new InvalidOperationException(
                "Cleanup batch size must be between 1 and 1000.");
        }
    }

    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Information,
        Message =
            "Listing maintenance completed. Expired: {ExpiredCount}, purged images: {PurgedCount}, failed images: {FailedCount}")]
    private static partial void LogMaintenanceCompleted(
        ILogger logger,
        int expiredCount,
        int purgedCount,
        int failedCount);

    [LoggerMessage(
        EventId = 4003,
        Level = LogLevel.Information,
        Message = "Expiry reminders written: {Count}")]
    private static partial void LogRemindersWritten(
        ILogger logger,
        int count);

    [LoggerMessage(
        EventId = 4002,
        Level = LogLevel.Error,
        Message = "Listing maintenance failed")]
    private static partial void LogMaintenanceFailure(
        ILogger logger,
        Exception exception);
}