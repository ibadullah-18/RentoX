using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Authentication;
using RentoX.Domain.Authentication;
using RentoX.Domain.Common.Exceptions;
using RentoX.Infrastructure.Authentication;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Persistence;

namespace RentoX.IntegrationTests.Authentication;

public sealed class RefreshTokenSecurityTests(
    AuthenticationPostgresFixture fixture)
    : IClassFixture<AuthenticationPostgresFixture>
{
    private static readonly DateTimeOffset TestNow =
        new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RotationLinksReplacementAndRejectsOldToken()
    {
        await using ServiceProvider provider = CreateProvider();
        TestSession session = await SeedAsync(provider, "+994500001001");

        AuthTokenResult replacement = Assert.IsType<AuthTokenResult>(
            await RefreshAsync(provider, session.Tokens.RefreshToken));

        Assert.Null(
            await RefreshAsync(provider, session.Tokens.RefreshToken));

        await using RentoXDbContext db = fixture.CreateContext();
        List<RefreshToken> tokens = await db.RefreshTokens
            .Where(token => token.UserId == session.UserId)
            .ToListAsync();

        Assert.Equal(2, tokens.Count);

        RefreshToken original = Assert.Single(
            tokens,
            token => token.TokenHash == HashToken(session.Tokens.RefreshToken));

        RefreshToken current = Assert.Single(
            tokens,
            token => token.TokenHash == HashToken(replacement.RefreshToken));

        Assert.NotNull(original.RevokedAtUtc);
        Assert.Equal((Guid?)current.Id, original.ReplacedByTokenId);
        Assert.Null(current.RevokedAtUtc);
    }

    [Fact]
    public async Task ConcurrentRefreshRequestsHaveOnlyOneWinner()
    {
        await using ServiceProvider provider = CreateProvider();
        TestSession session = await SeedAsync(provider, "+994500001002");

        TaskCompletionSource start =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        Task<AuthTokenResult?>[] requests = Enumerable.Range(0, 4)
            .Select(async _ =>
            {
                await start.Task;
                return await RefreshAsync(
                    provider,
                    session.Tokens.RefreshToken);
            })
            .ToArray();

        start.SetResult();

        AuthTokenResult?[] results = await Task.WhenAll(requests)
            .WaitAsync(TimeSpan.FromSeconds(30));

        Assert.Single(results, result => result is not null);

        await using RentoXDbContext db = fixture.CreateContext();

        Assert.Equal(
            2,
            await db.RefreshTokens.CountAsync(
                token => token.UserId == session.UserId));

        Assert.Equal(
            1,
            await db.RefreshTokens.CountAsync(
                token =>
                    token.UserId == session.UserId &&
                    token.RevokedAtUtc == null &&
                    token.ExpiresAtUtc > TestNow));
    }

    [Fact]
    public async Task UserCannotRevokeAnotherUsersSessions()
    {
        await using ServiceProvider provider = CreateProvider();

        TestSession first = await SeedAsync(provider, "+994500001003");
        TestSession second = await SeedAsync(provider, "+994500001004");

        await using (AsyncServiceScope scope = provider.CreateAsyncScope())
        {
            AuthSessionService service =
                CreateSessionService(scope.ServiceProvider, first.UserId);

            Assert.False(
                await service.RevokeAsync(second.Tokens.RefreshToken));

            await Assert.ThrowsAsync<DomainException>(
                () => service.RevokeAllAsync(second.UserId));
        }

        Assert.NotNull(
            await RefreshAsync(provider, second.Tokens.RefreshToken));
    }

    [Fact]
    public async Task LogoutUsingRotatedTokenRevokesItsReplacement()
    {
        await using ServiceProvider provider = CreateProvider();
        TestSession session = await SeedAsync(provider, "+994500001005");

        AuthTokenResult replacement = Assert.IsType<AuthTokenResult>(
            await RefreshAsync(provider, session.Tokens.RefreshToken));

        await using (AsyncServiceScope scope = provider.CreateAsyncScope())
        {
            AuthSessionService service =
                CreateSessionService(scope.ServiceProvider, session.UserId);

            Assert.True(
                await service.RevokeAsync(session.Tokens.RefreshToken));

            Assert.True(
                await service.RevokeAsync(session.Tokens.RefreshToken));
        }

        Assert.Null(
            await RefreshAsync(provider, replacement.RefreshToken));
    }

    [Fact]
    public async Task LogoutAllRacingRefreshLeavesNoActiveRefreshTokens()
    {
        await using ServiceProvider provider = CreateProvider();
        TestSession session = await SeedAsync(provider, "+994500001006");

        await using (AsyncServiceScope scope = provider.CreateAsyncScope())
        {
            await CreateTokenService(scope.ServiceProvider).CreateAsync(
                session.UserId,
                session.PhoneNumber);
        }

        TaskCompletionSource start =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task RefreshSessionAsync()
        {
            await start.Task;
            await RefreshAsync(provider, session.Tokens.RefreshToken);
        }

        async Task LogoutSessionsAsync()
        {
            await start.Task;

            await using AsyncServiceScope scope =
                provider.CreateAsyncScope();

            await CreateSessionService(
                    scope.ServiceProvider,
                    session.UserId)
                .RevokeAllAsync(session.UserId);
        }

        Task refresh = RefreshSessionAsync();
        Task logout = LogoutSessionsAsync();

        start.SetResult();

        await Task.WhenAll(refresh, logout)
            .WaitAsync(TimeSpan.FromSeconds(30));

        await using RentoXDbContext db = fixture.CreateContext();

        Assert.False(
            await db.RefreshTokens.AnyAsync(token =>
                token.UserId == session.UserId &&
                token.RevokedAtUtc == null &&
                token.ExpiresAtUtc > TestNow));
    }

    [Fact]
    public async Task FailureAfterTokenCreationRollsBackRotation()
    {
        await using ServiceProvider provider = CreateProvider();
        TestSession session = await SeedAsync(provider, "+994500001007");

        await using (AsyncServiceScope scope = provider.CreateAsyncScope())
        {
            IServiceProvider services = scope.ServiceProvider;

            RefreshTokenService service = new(
                services.GetRequiredService<RentoXDbContext>(),
                new ThrowingTokenService(CreateTokenService(services)),
                new FixedClock());

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.RefreshAsync(session.Tokens.RefreshToken));
        }

        await using (RentoXDbContext db = fixture.CreateContext())
        {
            List<RefreshToken> tokens = await db.RefreshTokens
                .Where(token => token.UserId == session.UserId)
                .ToListAsync();

            RefreshToken original = Assert.Single(tokens);

            Assert.Null(original.RevokedAtUtc);
            Assert.Null(original.ReplacedByTokenId);
            Assert.Equal(
                HashToken(session.Tokens.RefreshToken),
                original.TokenHash);
        }

        Assert.NotNull(
            await RefreshAsync(provider, session.Tokens.RefreshToken));
    }

    private ServiceProvider CreateProvider()
    {
        ServiceCollection services = new();

        services.AddLogging();

        services.AddScoped<RentoXDbContext>(
            _ => fixture.CreateContext());

        services.AddIdentityCore<AppUser>()
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<RentoXDbContext>();

        return services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateScopes = true
            });
    }

    private static async Task<TestSession> SeedAsync(
        ServiceProvider provider,
        string phoneNumber)
    {
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        RentoXDbContext db =
            services.GetRequiredService<RentoXDbContext>();

        Guid userId = Guid.NewGuid();

        db.Users.Add(new AppUser
        {
            Id = userId,
            UserName = phoneNumber,
            NormalizedUserName = phoneNumber,
            PhoneNumber = phoneNumber,
            PhoneNumberConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            RegisteredAtUtc = TestNow
        });

        await db.SaveChangesAsync();

        AuthTokenResult tokens =
            await CreateTokenService(services).CreateAsync(
                userId,
                phoneNumber);

        return new TestSession(userId, phoneNumber, tokens);
    }

    private static async Task<AuthTokenResult?> RefreshAsync(
        ServiceProvider provider,
        string refreshToken)
    {
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        RefreshTokenService service = new(
            services.GetRequiredService<RentoXDbContext>(),
            CreateTokenService(services),
            new FixedClock());

        return await service.RefreshAsync(refreshToken);
    }

    private static JwtTokenService CreateTokenService(
        IServiceProvider services)
    {
        return new JwtTokenService(
            new JwtOptions
            {
                Issuer = "RentoX.Api",
                Audience = "RentoX.Clients",
                SigningKey = new string('s', 64)
            },
            new FixedClock(),
            services.GetRequiredService<RentoXDbContext>(),
            services.GetRequiredService<UserManager<AppUser>>());
    }

    private static AuthSessionService CreateSessionService(
        IServiceProvider services,
        Guid userId)
    {
        return new AuthSessionService(
            services.GetRequiredService<RentoXDbContext>(),
            new FixedClock(),
            new TestCurrentUser(userId));
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private sealed record TestSession(
        Guid UserId,
        string PhoneNumber,
        AuthTokenResult Tokens);

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => TestNow;
    }

    private sealed class TestCurrentUser(Guid userId)
        : ICurrentUserContext
    {
        public Guid? UserId => userId;

        public bool IsAuthenticated => userId != Guid.Empty;
    }

    private sealed class ThrowingTokenService(ITokenService inner)
        : ITokenService
    {
        public async Task<AuthTokenResult> CreateAsync(
            Guid userId,
            string phoneNumber,
            CancellationToken cancellationToken = default)
        {
            await inner.CreateAsync(
                userId,
                phoneNumber,
                cancellationToken);

            throw new InvalidOperationException(
                "Simulated failure after refresh token persistence.");
        }
    }
}
