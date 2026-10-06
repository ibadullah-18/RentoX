using System.Threading.RateLimiting;
using Microsoft.AspNetCore.SignalR;

namespace RentoX.Api.Messaging;

public sealed class ConversationRateLimitFilter
    : IHubFilter, IDisposable
{
    private readonly PartitionedRateLimiter<CallKey> _limiter =
        PartitionedRateLimiter.Create<CallKey, CallKey>(
            key => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: key,
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = partition.IsHeartbeat ? 12 : 60,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        ArgumentNullException.ThrowIfNull(invocationContext);
        ArgumentNullException.ThrowIfNull(next);

        HubCallerContext caller = invocationContext.Context;

        caller.ConnectionAborted.ThrowIfCancellationRequested();

        if (caller.User?.Identity?.IsAuthenticated != true ||
            !Guid.TryParse(caller.UserIdentifier, out Guid userId) ||
            userId == Guid.Empty)
        {
            throw new HubException("Authentication is required.");
        }

        bool isHeartbeat = string.Equals(
            invocationContext.HubMethodName,
            nameof(ConversationsHub.Heartbeat),
            StringComparison.Ordinal);

        using RateLimitLease lease = _limiter.AttemptAcquire(
            new CallKey(userId, isHeartbeat));

        if (!lease.IsAcquired)
        {
            throw new HubException(
                "Too many live requests. Please wait and try again.");
        }

        return await next(invocationContext);
    }

    public void Dispose()
    {
        _limiter.Dispose();
        GC.SuppressFinalize(this);
    }

    private readonly record struct CallKey(
        Guid UserId,
        bool IsHeartbeat);
}
