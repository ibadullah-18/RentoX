using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RentoX.Application.Authentication;

namespace RentoX.Infrastructure.Authentication;

/// <summary>
/// Does NOT send an SMS: it writes the code into the server log. Allowed in Development, and in a real server only
/// when <c>Sms:LogCodes</c> is true, which is meant for the first private days before an SMS provider is connected:
/// the owner reads the codes from the log (<c>docker compose logs api</c>) to sign in and try the site. Nobody else
/// receives a code, so nobody else can sign in. Switch it off (and connect a real provider) before the site opens.
/// </summary>
public sealed partial class DevelopmentSmsSender(
    ILogger<DevelopmentSmsSender> logger,
    IHostEnvironment environment,
    IConfiguration configuration)
    : ISmsSender
{
    public Task SendOtpAsync(
        string phoneNumber,
        string code,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!environment.IsDevelopment() && !configuration.GetValue<bool>("Sms:LogCodes"))
        {
            throw new InvalidOperationException(
                "No SMS provider is configured. Connect one, or (only for private tests) set Sms:LogCodes=true " +
                "to write the codes into the server log.");
        }

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
