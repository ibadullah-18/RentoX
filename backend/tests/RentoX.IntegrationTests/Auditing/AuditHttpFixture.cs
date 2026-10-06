using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using RentoX.Api.Controllers.Admin;
using RentoX.Infrastructure.Persistence;
using RentoX.Infrastructure.Persistence.Interceptors;

namespace RentoX.IntegrationTests.Auditing;

public sealed class AuditHttpFixture :
    IAsyncLifetime,
    IAsyncDisposable
{
    private readonly AuditPostgresFixture database = new();

    private readonly IContainer redis =
        new ContainerBuilder("redis:7-alpine")
            .WithPortBinding(6379, true)
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilMessageIsLogged("Ready to accept connections"))
            .Build();

    private readonly string signingKey =
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private AuditApiFactory? factory;

    public async Task InitializeAsync()
    {
        await database.InitializeAsync();

        using var timeout =
            new CancellationTokenSource(TimeSpan.FromMinutes(5));

        await redis.StartAsync(timeout.Token);

        await using RentoXDbContext db = database.CreateContext();

        string connectionString =
            db.Database.GetConnectionString()
            ?? throw new InvalidOperationException(
                "The test database connection string is missing.");

        string redisConnection = string.Create(
            CultureInfo.InvariantCulture,
            $"{redis.Hostname}:{redis.GetMappedPublicPort(6379)},abortConnect=false");

        factory = new AuditApiFactory(
            connectionString,
            redisConnection,
            signingKey);

        // Starts the real API entry point and its middleware.
        using HttpClient client = CreateClient();
    }

    public HttpClient CreateClient()
    {
        AuditApiFactory currentFactory =
            factory ?? throw new InvalidOperationException(
                "The HTTP fixture has not been initialized.");

        return currentFactory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });
    }

    public RentoXDbContext CreateContext()
    {
        return database.CreateContext();
    }

    public string CreateToken(
        string role,
        bool expired = false,
        bool invalidSignature = false)
    {
        string key = invalidSignature
            ? Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            : signingKey;

        DateTime now = DateTime.UtcNow;

        Claim[] claims =
        [
            new Claim(
                ClaimTypes.NameIdentifier,
                Guid.NewGuid().ToString("D")),
            new Claim(ClaimTypes.Role, role)
        ];

        var token = new JwtSecurityToken(
            issuer: "RentoX.Api",
            audience: "RentoX.Clients",
            claims: claims,
            notBefore: now.AddHours(-1),
            expires: expired
                ? now.AddMinutes(-10)
                : now.AddMinutes(10),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        return DisposeAsync().AsTask();
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (factory is not null)
            {
                await factory.DisposeAsync();
            }
        }
        finally
        {
            try
            {
                await redis.DisposeAsync();
            }
            finally
            {
                await database.DisposeAsync();
            }
        }

        GC.SuppressFinalize(this);
    }

    private sealed class AuditApiFactory(
        string databaseConnection,
        string redisConnection,
        string jwtSigningKey)
        : WebApplicationFactory<AdminAuditLogsController>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.UseSetting(
                "ConnectionStrings:Database",
                databaseConnection);

            builder.UseSetting(
                "ConnectionStrings:Redis",
                redisConnection);

            builder.UseSetting("Jwt:SigningKey", jwtSigningKey);

            builder.UseSetting(
                "Otp:HashingKey",
                jwtSigningKey);

            builder.UseSetting(
                "IdentitySeed:SuperAdminPhoneNumber",
                string.Empty);

            builder.UseSetting(
                "PushNotifications:Enabled",
                "false");

            // Every DbContext resolution, including startup seeding,
            // is explicitly bound to the isolated test database.
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<RentoXDbContext>();
                services.RemoveAll<DbContextOptions<RentoXDbContext>>();
                services.RemoveAll<
                    IDbContextOptionsConfiguration<RentoXDbContext>>();

                services.AddDbContext<RentoXDbContext>(
                    (provider, options) =>
                    {
                        options.UseNpgsql(databaseConnection);

                        options.AddInterceptors(
                            provider.GetRequiredService<
                                AuditableEntityInterceptor>());
                    });
            });
        }
    }
}