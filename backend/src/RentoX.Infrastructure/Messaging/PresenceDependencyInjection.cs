using Microsoft.Extensions.DependencyInjection;
using RentoX.Application.Messaging;
using RentoX.Infrastructure.Messaging;
using StackExchange.Redis;

namespace RentoX.Infrastructure;

public static class PresenceDependencyInjection
{
    public static IServiceCollection AddRentoXPresence(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            ConfigurationOptions options =
                ConfigurationOptions.Parse(connectionString);

            options.AbortOnConnectFail = false;
            options.ConnectTimeout = 3000;
            options.AsyncTimeout = 3000;
            options.ConnectRetry = 1;
            options.BacklogPolicy = BacklogPolicy.FailFast;

            return ConnectionMultiplexer.Connect(options);
        });

        services.AddSingleton<
            IUserPresenceStore,
            RedisUserPresenceStore>();

        return services;
    }
}
