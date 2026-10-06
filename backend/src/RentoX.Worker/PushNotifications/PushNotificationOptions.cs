namespace RentoX.Worker.PushNotifications;

public sealed class PushNotificationOptions
{
    public const string SectionName =
        "PushNotifications";

    public bool Enabled { get; set; }

    public int IntervalSeconds { get; set; } = 15;

    public int BatchSize { get; set; } = 50;

    public int MaximumAttempts { get; set; } = 5;

    public int ProcessingTimeoutMinutes { get; set; } = 5;
}