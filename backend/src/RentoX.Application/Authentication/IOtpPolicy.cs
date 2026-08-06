namespace RentoX.Application.Authentication;

public interface IOtpPolicy
{
    TimeSpan Lifetime { get; }

    TimeSpan ResendInterval { get; }

    int MaximumAttempts { get; }
}