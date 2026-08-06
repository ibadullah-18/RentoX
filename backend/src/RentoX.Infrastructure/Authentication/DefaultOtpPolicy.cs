using RentoX.Application.Authentication;

namespace RentoX.Infrastructure.Authentication;

public sealed class DefaultOtpPolicy(
    OtpOptions options) : IOtpPolicy
{
    public TimeSpan Lifetime => options.Lifetime;

    public TimeSpan ResendInterval =>
        options.ResendInterval;

    public int MaximumAttempts =>
        options.MaximumAttempts;
}