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

    /// <summary>How many days before the end the owner is reminded (0: no reminders).</summary>
    public int ExpiryReminderDays { get; init; } = 3;
}