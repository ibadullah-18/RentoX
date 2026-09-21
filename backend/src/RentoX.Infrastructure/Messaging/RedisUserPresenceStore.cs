using Microsoft.Extensions.Logging;
using RentoX.Application.Messaging;
using StackExchange.Redis;

namespace RentoX.Infrastructure.Messaging;

public sealed class RedisUserPresenceStore(
    IConnectionMultiplexer redis,
    ILogger<RedisUserPresenceStore> logger)
    : IUserPresenceStore
{
    private static readonly Action<ILogger, Exception?>
        LogUnavailable = LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(2101, "PresenceRedisUnavailable"),
            "Presence Redis is unavailable.");

    private const string RefreshScript = """
        local clock = redis.call('TIME')
        local now = tonumber(clock[1])
        redis.call('ZREMRANGEBYSCORE', KEYS[1], '-inf', now)
        redis.call('ZADD', KEYS[1], now + 60, ARGV[1])
        redis.call('EXPIRE', KEYS[1], 90)
        return 1
        """;

    private const string RemoveScript = """
        local clock = redis.call('TIME')
        local now = tonumber(clock[1])
        redis.call('ZREMRANGEBYSCORE', KEYS[1], '-inf', now)
        redis.call('ZREM', KEYS[1], ARGV[1])
        return 1
        """;

    private const string ReadScript = """
        local clock = redis.call('TIME')
        local now = tonumber(clock[1])
        redis.call('ZREMRANGEBYSCORE', KEYS[1], '-inf', now)
        return redis.call('ZCARD', KEYS[1])
        """;

    public async Task<bool> RefreshAsync(
        Guid userId,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        RedisResult? result = await ExecuteAsync(
            RefreshScript,
            userId,
            connectionId,
            cancellationToken);

        return result is not null;
    }

    public async Task RemoveAsync(
        Guid userId,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        await ExecuteAsync(
            RemoveScript,
            userId,
            connectionId,
            cancellationToken);
    }

    public async Task<bool?> IsOnlineAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        RedisResult? result = await ExecuteAsync(
            ReadScript,
            userId,
            null,
            cancellationToken);

        if (result is null)
        {
            return null;
        }

        return (long)result > 0;
    }

    private async Task<RedisResult?> ExecuteAsync(
        string script,
        Guid userId,
        string? connectionId,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User id is required.",
                nameof(userId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        RedisKey[] keys =
        [
            $"rentox:presence:v1:{userId:N}"
        ];

        RedisValue[] values =
            connectionId is null
                ? []
                : [connectionId];

        try
        {
            return await redis.GetDatabase()
                .ScriptEvaluateAsync(script, keys, values)
                .WaitAsync(cancellationToken);
        }
        catch (RedisException exception)
        {
            LogUnavailable(logger, exception);
            return null;
        }
    }
}
