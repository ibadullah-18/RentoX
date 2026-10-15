using RentoX.Application.Hr;

namespace RentoX.Api.Hr;

/// <summary>
/// Every couple of minutes: who should have started work and has not? The owner (and whoever looks after the
/// staff) is told once per person and day. It lives in the API so the alert can reach an open panel at once.
/// </summary>
public sealed class AttendanceMonitor(IServiceScopeFactory scopes, ILogger<AttendanceMonitor> logger) : BackgroundService
{
    private static readonly TimeSpan Every = TimeSpan.FromMinutes(2);

    private static readonly Action<ILogger, Exception?> Failed =
        LoggerMessage.Define(LogLevel.Warning, new EventId(1501, "AttendanceCheckFailed"), "The attendance check failed");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let the API finish starting before the first look.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using PeriodicTimer timer = new(Every);
        do
        {
            try
            {
                await using AsyncServiceScope scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IStaffHrService>().CheckAbsencesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                Failed(logger, exception);
            }
        }
        while (await WaitAsync(timer, stoppingToken));
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken token)
    {
        try
        {
            return await timer.WaitForNextTickAsync(token);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
