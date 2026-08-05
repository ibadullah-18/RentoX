namespace RentoX.Worker;

public sealed partial class Worker(
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            LogWorkerRunning(logger, DateTimeOffset.UtcNow);

            await Task.Delay(
                TimeSpan.FromMinutes(1),
                stoppingToken);
        }
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "RentoX worker running at {Time}")]
    private static partial void LogWorkerRunning(
        ILogger logger,
        DateTimeOffset time);
}