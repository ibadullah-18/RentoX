namespace RentoX.Worker;

public sealed class ListingMaintenanceOptions
{
    public int IntervalMinutes { get; init; } = 5;

    public int DeletedImageRetentionDays
    {
        get;
        init;
    } = 30;

    public int CleanupBatchSize { get; init; } = 100;
}