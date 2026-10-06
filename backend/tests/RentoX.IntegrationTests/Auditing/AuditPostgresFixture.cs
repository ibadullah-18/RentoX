using Microsoft.EntityFrameworkCore;
using RentoX.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace RentoX.IntegrationTests.Auditing;

public sealed class AuditPostgresFixture :
    IAsyncLifetime,
    IAsyncDisposable
{
    private readonly PostgreSqlContainer container =
        new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("rentox_audit_tests")
            .WithUsername("rentox_test")
            .WithPassword("rentox-isolated-test-password")
            .Build();

    public RentoXDbContext CreateContext()
    {
        DbContextOptions<RentoXDbContext> options =
            new DbContextOptionsBuilder<RentoXDbContext>()
                .UseNpgsql(container.GetConnectionString())
                .Options;

        return new RentoXDbContext(options);
    }

    public async Task InitializeAsync()
    {
        using var timeout =
            new CancellationTokenSource(TimeSpan.FromMinutes(5));

        await container.StartAsync(timeout.Token);

        await using RentoXDbContext db = CreateContext();

        await db.Database.MigrateAsync(timeout.Token);
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        return DisposeAsync().AsTask();
    }

    public async ValueTask DisposeAsync()
    {
        await container.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}