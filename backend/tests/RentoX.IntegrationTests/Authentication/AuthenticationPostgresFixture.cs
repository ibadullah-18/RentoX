using Microsoft.EntityFrameworkCore;
using RentoX.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace RentoX.IntegrationTests.Authentication;

public sealed class AuthenticationPostgresFixture
    : IAsyncLifetime, IAsyncDisposable
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("rentox_authentication_tests")
            .WithUsername("rentox_test")
            .WithPassword("rentox_test_password")
            .Build();

    public async Task InitializeAsync()
    {
        using CancellationTokenSource timeout =
            new(TimeSpan.FromMinutes(5));

        await _postgres.StartAsync(timeout.Token);

        await using RentoXDbContext dbContext = CreateContext();

        await dbContext.Database.MigrateAsync(timeout.Token);
    }

    public RentoXDbContext CreateContext()
    {
        DbContextOptions<RentoXDbContext> options =
            new DbContextOptionsBuilder<RentoXDbContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options;

        return new RentoXDbContext(options);
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        return DisposeAsync().AsTask();
    }

    public async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
