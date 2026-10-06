using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RentoX.Infrastructure.Authentication;

namespace RentoX.IntegrationTests.Authentication;

public sealed class DevelopmentSmsSafetyTests
{
    private const string TestPhone = "+994500000001";
    private const string TestCode = "123456";

    [Fact]
    public async Task DevelopmentAllowsLocalOtpLogging()
    {
        RecordingLogger logger = new();

        DevelopmentSmsSender sender = new(
            logger,
            new TestEnvironment("Development"));

        await sender.SendOtpAsync(TestPhone, TestCode);

        Assert.Equal(1, logger.EntryCount);

        Assert.Contains(
            TestCode,
            logger.LastMessage ?? string.Empty,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Testing")]
    [InlineData("")]
    public async Task OtherEnvironmentsRejectWithoutLoggingOtp(
        string environmentName)
    {
        RecordingLogger logger = new();

        DevelopmentSmsSender sender = new(
            logger,
            new TestEnvironment(environmentName));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendOtpAsync(TestPhone, TestCode));

        Assert.Equal(0, logger.EntryCount);
        Assert.Null(logger.LastMessage);
    }

    [Fact]
    public async Task CancelledRequestDoesNotLogOtp()
    {
        RecordingLogger logger = new();

        DevelopmentSmsSender sender = new(
            logger,
            new TestEnvironment("Development"));

        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sender.SendOtpAsync(
                TestPhone,
                TestCode,
                cancellation.Token));

        Assert.Equal(0, logger.EntryCount);
        Assert.Null(logger.LastMessage);
    }

    private sealed class TestEnvironment(string environmentName)
        : IHostEnvironment
    {
        public string EnvironmentName { get; set; } =
            environmentName;

        public string ApplicationName { get; set; } =
            "RentoX.Tests";

        public string ContentRootPath { get; set; } =
            AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } =
            new NullFileProvider();
    }

    private sealed class RecordingLogger
        : ILogger<DevelopmentSmsSender>
    {
        public int EntryCount { get; private set; }

        public string? LastMessage { get; private set; }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            EntryCount++;
            LastMessage = formatter(state, exception);
        }
    }
}
