using Microsoft.Extensions.Logging;
using RentoX.Application.Authentication;

namespace RentoX.Infrastructure.Authentication;

public sealed partial class DevelopmentSmsSender(
    ILogger<DevelopmentSmsSender> logger)
    : ISmsSender
{
    public Task SendOtpAsync(
        string phoneNumber,
        string code,
        CancellationToken cancellationToken = default)
    {
        LogDevelopmentOtp(
            logger,
            phoneNumber,
            code);

        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Warning,
        Message = "DEVELOPMENT OTP for {PhoneNumber}: {Code}")]
    private static partial void LogDevelopmentOtp(
        ILogger logger,
        string phoneNumber,
        string code);
}