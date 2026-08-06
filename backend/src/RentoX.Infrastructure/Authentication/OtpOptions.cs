namespace RentoX.Infrastructure.Authentication;

public sealed class OtpOptions
{
    public required string HashingKey { get; init; }

    public int CodeLength { get; init; } = 6;

    public TimeSpan Lifetime { get; init; } =
        TimeSpan.FromMinutes(5);

    public int MaximumAttempts { get; init; } = 5;

    public TimeSpan ResendInterval { get; init; } =
        TimeSpan.FromSeconds(60);
}