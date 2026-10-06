using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Authentication;
using RentoX.Application.Authorization;
using RentoX.Domain.Authentication;
using RentoX.Domain.Authentication.Enums;
using RentoX.Domain.Common.Exceptions;
using RentoX.Infrastructure.Authentication;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Persistence;

namespace RentoX.IntegrationTests.Authentication;

public sealed class OtpConcurrencyTests(
    AuthenticationPostgresFixture fixture)
    : IClassFixture<AuthenticationPostgresFixture>
{
    private static readonly DateTimeOffset TestNow =
        new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ParallelCorrectLoginCreatesOnlyOneRefreshToken()
    {
        const string phone = "+994500000011";

        Guid userId = await CreateUserAsync(phone);
        Guid challengeId = await CreateChallengeAsync(
            phone, OtpPurpose.Login);

        await using ServiceProvider provider = CreateProvider();

        bool[] results = await RunTogetherAsync(
            2,
            () => TryLoginAsync(provider, challengeId, "123456"));

        Assert.Single(results, succeeded => succeeded);

        await using RentoXDbContext read = fixture.CreateContext();

        Assert.Equal(
            1,
            await read.RefreshTokens.CountAsync(
                token => token.UserId == userId));

        OtpChallenge challenge =
            await read.OtpChallenges.AsNoTracking()
                .SingleAsync(item => item.Id == challengeId);

        Assert.NotNull(challenge.VerifiedAtUtc);
    }

    [Fact]
    public async Task ParallelWrongCodesPreserveAllFiveAttempts()
    {
        const string phone = "+994500000012";

        Guid userId = await CreateUserAsync(phone);
        Guid challengeId = await CreateChallengeAsync(
            phone, OtpPurpose.Login);

        await using ServiceProvider provider = CreateProvider();

        bool[] results = await RunTogetherAsync(
            5,
            () => TryLoginAsync(provider, challengeId, "654321"));

        Assert.All(results, succeeded => Assert.False(succeeded));

        await using RentoXDbContext read = fixture.CreateContext();

        OtpChallenge challenge =
            await read.OtpChallenges.AsNoTracking()
                .SingleAsync(item => item.Id == challengeId);

        Assert.Equal(5, challenge.FailedAttemptCount);
        Assert.Null(challenge.VerifiedAtUtc);

        Assert.Equal(
            0,
            await read.RefreshTokens.CountAsync(
                token => token.UserId == userId));
    }

    [Fact]
    public async Task ParallelOtpRequestsCreateAndSendOnlyOneCode()
    {
        const string phone = "+994500000013";

        CountingSmsSender sender = new();

        bool[] results = await RunTogetherAsync(
            2,
            async () =>
            {
                await using RentoXDbContext db = fixture.CreateContext();

                LoginOtpService service = new(
                    new OtpChallengeRepository(db),
                    new FixedCodeGenerator(),
                    CreateHasher(),
                    sender,
                    new KnownPhoneLookup(true),
                    new DefaultOtpPolicy(CreateOtpOptions()),
                    new FixedClock(),
                    db,
                    new OtpOperationScopeFactory(db));

                try
                {
                    await service.RequestAsync(phone);
                    return true;
                }
                catch (DomainException)
                {
                    return false;
                }
            });

        Assert.Single(results, succeeded => succeeded);
        Assert.Equal(1, sender.SendCount);

        await using RentoXDbContext read = fixture.CreateContext();

        Assert.Equal(
            1,
            await read.OtpChallenges.CountAsync(
                challenge =>
                    challenge.PhoneNumber == phone &&
                    challenge.Purpose == OtpPurpose.Login));
    }

    [Fact]
    public async Task TokenFailureRollsBackOtpAndSavedRefreshToken()
    {
        const string phone = "+994500000014";

        Guid userId = await CreateUserAsync(phone);
        Guid challengeId = await CreateChallengeAsync(
            phone, OtpPurpose.Login);

        await using ServiceProvider provider = CreateProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        RentoXDbContext db =
            scope.ServiceProvider.GetRequiredService<RentoXDbContext>();

        UserManager<AppUser> manager =
            scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        ITokenService tokens = new FailAfterTokenService(
            CreateTokenService(db, manager));

        CompleteLoginService service =
            CreateLoginService(db, manager, tokens);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CompleteAsync(challengeId, "123456"));

        Assert.Equal("Simulated failure after token save.", exception.Message);

        await using RentoXDbContext read = fixture.CreateContext();

        OtpChallenge challenge =
            await read.OtpChallenges.AsNoTracking()
                .SingleAsync(item => item.Id == challengeId);

        Assert.Null(challenge.VerifiedAtUtc);

        Assert.Equal(
            0,
            await read.RefreshTokens.CountAsync(
                token => token.UserId == userId));
    }

    [Fact]
    public async Task RoleFailureRollsBackRegistrationAndOtp()
    {
        const string phone = "+994500000015";

        Guid challengeId = await CreateChallengeAsync(
            phone, OtpPurpose.Registration);

        await using ServiceProvider provider = CreateProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        RentoXDbContext db =
            scope.ServiceProvider.GetRequiredService<RentoXDbContext>();

        UserManager<AppUser> manager =
            scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        CompleteRegistrationService service = new(
            challengeRepository: new OtpChallengeRepository(db),
            codeHasher: CreateHasher(),
            accountService: new RegistrationAccountService(
                db, manager, new FixedClock()),
            identityUserLookup: new KnownPhoneLookup(false),
            clock: new FixedClock(),
            tokenService: CreateTokenService(db, manager),
            userRoleService: new FailingRoleService(),
            unitOfWork: db,
            operations: new OtpOperationScopeFactory(db));

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CompleteAsync(
                    challengeId,
                    "123456",
                    "OTP Test User",
                    1));

        Assert.Equal("Simulated role failure.", exception.Message);

        await using RentoXDbContext read = fixture.CreateContext();

        Assert.False(
            await read.Users.AnyAsync(user => user.PhoneNumber == phone));

        OtpChallenge challenge =
            await read.OtpChallenges.AsNoTracking()
                .SingleAsync(item => item.Id == challengeId);

        Assert.Null(challenge.VerifiedAtUtc);
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

    private async Task<Guid> CreateUserAsync(string phone)
    {
        AppUser user = new()
        {
            Id = Guid.NewGuid(),
            UserName = phone,
            NormalizedUserName = phone,
            PhoneNumber = phone,
            PhoneNumberConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            RegisteredAtUtc = TestNow
        };

        await using RentoXDbContext db = fixture.CreateContext();

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user.Id;
    }

    private async Task<Guid> CreateChallengeAsync(
        string phone,
        OtpPurpose purpose)
    {
        OtpChallenge challenge = OtpChallenge.Create(
            PhoneNumber.Create(phone),
            CreateHasher().Hash(phone, "123456"),
            purpose,
            TestNow,
            TimeSpan.FromMinutes(5),
            maximumAttempts: 5);

        await using RentoXDbContext db = fixture.CreateContext();

        db.OtpChallenges.Add(challenge);
        await db.SaveChangesAsync();

        return challenge.Id;
    }

    private static async Task<bool> TryLoginAsync(
        ServiceProvider provider,
        Guid challengeId,
        string code)
    {
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        RentoXDbContext db =
            scope.ServiceProvider.GetRequiredService<RentoXDbContext>();

        UserManager<AppUser> manager =
            scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        CompleteLoginService service = CreateLoginService(db, manager);

        try
        {
            await service.CompleteAsync(challengeId, code);
            return true;
        }
        catch (DomainException)
        {
            return false;
        }
    }

    private static CompleteLoginService CreateLoginService(
        RentoXDbContext db,
        UserManager<AppUser> manager,
        ITokenService? tokens = null)
    {
        return new CompleteLoginService(
            challengeRepository: new OtpChallengeRepository(db),
            codeHasher: CreateHasher(),
            loginAccountService: new LoginAccountService(db),
            clock: new FixedClock(),
            unitOfWork: db,
            tokenService: tokens ?? CreateTokenService(db, manager),
            operations: new OtpOperationScopeFactory(db));
    }

    private static JwtTokenService CreateTokenService(
        RentoXDbContext db,
        UserManager<AppUser> manager)
    {
        return new JwtTokenService(
            new JwtOptions
            {
                Issuer = "RentoX.Api",
                Audience = "RentoX.Clients",
                SigningKey = new string('s', 64)
            },
            new FixedClock(),
            db,
            manager);
    }

    private static OtpOptions CreateOtpOptions()
    {
        return new OtpOptions
        {
            HashingKey = new string('k', 32)
        };
    }

    private static HmacOtpCodeHasher CreateHasher()
    {
        return new HmacOtpCodeHasher(CreateOtpOptions());
    }

    private static async Task<bool[]> RunTogetherAsync(
        int count,
        Func<Task<bool>> action)
    {
        TaskCompletionSource start =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        Task<bool>[] tasks = Enumerable.Range(0, count)
            .Select(async _ =>
            {
                await start.Task;
                return await action();
            })
            .ToArray();

        start.SetResult();

        return await Task.WhenAll(tasks)
            .WaitAsync(TimeSpan.FromSeconds(30));
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => TestNow;
    }

    private sealed class KnownPhoneLookup(bool exists)
        : IIdentityUserLookup
    {
        public Task<bool> PhoneExistsAsync(
            string phoneNumber,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(exists);
        }
    }

    private sealed class FixedCodeGenerator : IOtpCodeGenerator
    {
        public string Generate() => "123456";
    }

    private sealed class CountingSmsSender : ISmsSender
    {
        private int _sendCount;

        public int SendCount => Volatile.Read(ref _sendCount);

        public Task SendOtpAsync(
            string phoneNumber,
            string code,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _sendCount);
            return Task.CompletedTask;
        }
    }

    private sealed class FailAfterTokenService(ITokenService inner)
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
                "Simulated failure after token save.");
        }
    }

    private sealed class FailingRoleService : IUserRoleService
    {
        public Task AssignDefaultRoleAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Simulated role failure.");
        }
    }
}
