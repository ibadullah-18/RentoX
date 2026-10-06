using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using RentoX.Api.Messaging;

namespace RentoX.IntegrationTests.RateLimiting;

public sealed class ConversationRateLimitFilterTests
{
    [Fact]
    public async Task TypingMethodsAndConnectionsShareQuota()
    {
        using ConversationRateLimitFilter filter = new();
        using TestHub hub = new();

        Guid userId = Guid.NewGuid();

        HubInvocationContext first = CreateInvocation(
            hub, userId, nameof(TestHub.StartTyping), "connection-one");

        HubInvocationContext second = CreateInvocation(
            hub, userId, nameof(TestHub.StopTyping), "connection-two");

        int executed = 0;

        ValueTask<object?> Next(HubInvocationContext context)
        {
            executed++;
            return ValueTask.FromResult<object?>(context.HubMethodName);
        }

        for (int index = 0; index < 60; index++)
        {
            await filter.InvokeMethodAsync(
                index % 2 == 0 ? first : second,
                Next);
        }

        HubException exception = await Assert.ThrowsAsync<HubException>(
            () => filter.InvokeMethodAsync(second, Next).AsTask());

        Assert.Equal(
            "Too many live requests. Please wait and try again.",
            exception.Message);

        Assert.Equal(60, executed);
    }

    [Fact]
    public async Task AnotherUserHasIndependentQuota()
    {
        using ConversationRateLimitFilter filter = new();
        using TestHub hub = new();

        HubInvocationContext first = CreateInvocation(
            hub,
            Guid.NewGuid(),
            nameof(TestHub.StartTyping),
            "first-user");

        HubInvocationContext second = CreateInvocation(
            hub,
            Guid.NewGuid(),
            nameof(TestHub.StartTyping),
            "second-user");

        for (int index = 0; index < 60; index++)
        {
            await InvokeAsync(filter, first);
        }

        await Assert.ThrowsAsync<HubException>(
            () => InvokeAsync(filter, first));

        object? result = await InvokeAsync(filter, second);

        Assert.Equal("allowed", result);
    }

    [Fact]
    public async Task HeartbeatHasIndependentLimitedQuota()
    {
        using ConversationRateLimitFilter filter = new();
        using TestHub hub = new();

        Guid userId = Guid.NewGuid();

        HubInvocationContext typing = CreateInvocation(
            hub, userId, nameof(TestHub.StartTyping), "connection-one");

        HubInvocationContext heartbeat = CreateInvocation(
            hub, userId, nameof(TestHub.Heartbeat), "connection-one");

        for (int index = 0; index < 60; index++)
        {
            await InvokeAsync(filter, typing);
        }

        await Assert.ThrowsAsync<HubException>(
            () => InvokeAsync(filter, typing));

        for (int index = 0; index < 12; index++)
        {
            object? result = await InvokeAsync(filter, heartbeat);
            Assert.Equal("allowed", result);
        }

        await Assert.ThrowsAsync<HubException>(
            () => InvokeAsync(filter, heartbeat));
    }

    [Fact]
    public async Task UnauthenticatedCallerCannotExecuteMethod()
    {
        using ConversationRateLimitFilter filter = new();
        using TestHub hub = new();

        HubInvocationContext invocation = CreateInvocation(
            hub,
            Guid.NewGuid(),
            nameof(TestHub.StartTyping),
            "anonymous",
            isAuthenticated: false);

        bool executed = false;

        ValueTask<object?> Next(HubInvocationContext context)
        {
            executed = true;
            return ValueTask.FromResult<object?>(context.HubMethodName);
        }

        HubException exception = await Assert.ThrowsAsync<HubException>(
            () => filter.InvokeMethodAsync(invocation, Next).AsTask());

        Assert.Equal("Authentication is required.", exception.Message);
        Assert.False(executed);
    }

    private static Task<object?> InvokeAsync(
        ConversationRateLimitFilter filter,
        HubInvocationContext context)
    {
        return filter.InvokeMethodAsync(
            context,
            static _ => ValueTask.FromResult<object?>("allowed"))
            .AsTask();
    }

    private static HubInvocationContext CreateInvocation(
        TestHub hub,
        Guid userId,
        string methodName,
        string connectionId,
        bool isAuthenticated = true)
    {
        MethodInfo method =
            typeof(TestHub).GetMethod(methodName)
            ?? throw new InvalidOperationException(
                "Test hub method was not found.");

        return new HubInvocationContext(
            new TestCallerContext(
                userId,
                connectionId,
                isAuthenticated),
            new TestServices(),
            hub,
            method,
            Array.Empty<object?>());
    }

    private sealed class TestHub : Hub
    {
        public string StartTyping() => Context.ConnectionId;

        public string StopTyping() => Context.ConnectionId;

        public string Heartbeat() => Context.ConnectionId;
    }

    private sealed class TestServices : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            ArgumentNullException.ThrowIfNull(serviceType);
            return null;
        }
    }

    private sealed class TestCallerContext : HubCallerContext
    {
        public TestCallerContext(
            Guid userId,
            string connectionId,
            bool isAuthenticated)
        {
            ConnectionId = connectionId;
            UserIdentifier = userId.ToString();

            ClaimsIdentity identity = isAuthenticated
                ? new ClaimsIdentity(
                    new[]
                    {
                        new Claim(
                            ClaimTypes.NameIdentifier,
                            userId.ToString())
                    },
                    "Test")
                : new ClaimsIdentity();

            User = new ClaimsPrincipal(identity);
        }

        public override string ConnectionId { get; }

        public override string? UserIdentifier { get; }

        public override ClaimsPrincipal? User { get; }

        public override IDictionary<object, object?> Items { get; } =
            new Dictionary<object, object?>();

        public override IFeatureCollection Features { get; } =
            new FeatureCollection();

        public override CancellationToken ConnectionAborted =>
            CancellationToken.None;

        public override void Abort()
        {
        }
    }
}
