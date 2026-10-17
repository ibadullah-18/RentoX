using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using RentoX.Infrastructure.Authentication;

namespace RentoX.IntegrationTests.Files;

/// <summary>Sign-in codes without an SMS provider: only in Development, or on a server where the owner switched it on.</summary>
public sealed class SmsLogCodesTests
{
    [Fact]
    public async Task ARealServerWithoutTheSettingRefusesAndWithItWritesTheCodeToTheLog()
    {
        DevelopmentSmsSender off = Sender("Production", null);
        await Assert.ThrowsAsync<InvalidOperationException>(() => off.SendOtpAsync("+994501234567", "123456"));

        DevelopmentSmsSender explicitlyOff = Sender("Production", "false");
        await Assert.ThrowsAsync<InvalidOperationException>(() => explicitlyOff.SendOtpAsync("+994501234567", "123456"));

        await Sender("Production", "true").SendOtpAsync("+994501234567", "123456");
        await Sender("Development", null).SendOtpAsync("+994501234567", "123456");
    }

    private static DevelopmentSmsSender Sender(string environment, string? logCodes)
    {
        Dictionary<string, string?> settings = [];
        if (logCodes is not null)
        {
            settings["Sms:LogCodes"] = logCodes;
        }

        return new DevelopmentSmsSender(
            NullLogger<DevelopmentSmsSender>.Instance,
            new TestEnvironment(environment),
            new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
    }

    private sealed class TestEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;

        public string ApplicationName { get; set; } = "Test";

        public string ContentRootPath { get; set; } = Path.GetTempPath();

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
