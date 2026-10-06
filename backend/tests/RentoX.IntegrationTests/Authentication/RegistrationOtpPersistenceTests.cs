using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Authentication;
using RentoX.Application.Authorization;
using RentoX.Domain.Authentication;
using RentoX.Domain.Authentication.Enums;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Authentication;
using RentoX.Infrastructure.Persistence;

namespace RentoX.IntegrationTests.Authentication;

public sealed class RegistrationOtpPersistenceTests(
    AuthenticationPostgresFixture fixture)
    : IClassFixture<AuthenticationPostgresFixture>
{
    private const string CorrectCode = "123456";
    private const string WrongCode = "654321";

    private static readonly DateTimeOffset TestNow =
        new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task IncorrectCodePersistsAttemptAcrossContexts()
    {
        Guid challengeId = await CreateChallengeAsync("+994500000001");
        AccountDependencies dependencies = new();

        await using (RentoXDbContext requestContext =
                     fixture.CreateContext())
        {
            CompleteRegistrationService service =
                CreateService(requestContext, dependencies);

            DomainException exception =
                await Assert.ThrowsAsync<DomainException>(
                    () => service.CompleteAsync(
                        challengeId,
                        WrongCode,
                        "OTP Test User",
                        (int)PreferredLanguage.Azerbaijani));

            Assert.Equal(
                "OTP code is incorrect.",
                exception.Message);
        }

        await using RentoXDbContext readContext =
            fixture.CreateContext();

        OtpChallenge saved =
            await readContext.OtpChallenges
                .AsNoTracking()
                .SingleAsync(item => item.Id == challengeId);

        Assert.Equal(1, saved.FailedAttemptCount);
        Assert.Null(saved.VerifiedAtUtc);
        Assert.Equal(0, dependencies.UnexpectedCalls);
    }

    [Fact]
    public async Task FiveFailedRequestsBlockEvenTheCorrectCode()
    {
        Guid challengeId = await CreateChallengeAsync("+994500000002");
        AccountDependencies dependencies = new();

        for (int attempt = 1; attempt <= 5; attempt++)
        {
            await using RentoXDbContext requestContext =
                fixture.CreateContext();

            CompleteRegistrationService service =
                CreateService(requestContext, dependencies);

            DomainException exception =
                await Assert.ThrowsAsync<DomainException>(
                    () => service.CompleteAsync(
                        challengeId,
                        WrongCode,
                        "OTP Test User",
                        (int)PreferredLanguage.Azerbaijani));

            Assert.Equal(
                attempt == 5
                    ? "OTP attempt limit has been exceeded."
                    : "OTP code is incorrect.",
                exception.Message);
        }

        // A new context represents another request with the correct code.
        await using (RentoXDbContext correctRequestContext =
                     fixture.CreateContext())
        {
            CompleteRegistrationService service =
                CreateService(correctRequestContext, dependencies);

            DomainException exception =
                await Assert.ThrowsAsync<DomainException>(
                    () => service.CompleteAsync(
                        challengeId,
                        CorrectCode,
                        "OTP Test User",
                        (int)PreferredLanguage.Azerbaijani));

            Assert.Equal(
                "OTP attempt limit has been exceeded.",
                exception.Message);
        }

        await using RentoXDbContext readContext =
            fixture.CreateContext();

        OtpChallenge saved =
            await readContext.OtpChallenges
                .AsNoTracking()
                .SingleAsync(item => item.Id == challengeId);

        Assert.Equal(5, saved.FailedAttemptCount);
        Assert.Null(saved.VerifiedAtUtc);
        Assert.Equal(0, dependencies.UnexpectedCalls);
    }

    private async Task<Guid> CreateChallengeAsync(string phone)
    {
        PhoneNumber phoneNumber =
            PhoneNumber.Create(phone);

        HmacOtpCodeHasher hasher = CreateHasher();

        OtpChallenge challenge = OtpChallenge.Create(
            phoneNumber,
            hasher.Hash(phoneNumber.Value, CorrectCode),
            OtpPurpose.Registration,
            TestNow,
            TimeSpan.FromMinutes(5),
            maximumAttempts: 5);

        await using RentoXDbContext dbContext =
            fixture.CreateContext();

        dbContext.OtpChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        return challenge.Id;
    }

    private static CompleteRegistrationService CreateService(
        RentoXDbContext dbContext,
        AccountDependencies dependencies)
    {
        return new CompleteRegistrationService(
            challengeRepository: new OtpChallengeRepository(dbContext),
            codeHasher: CreateHasher(),
            accountService: dependencies,
            identityUserLookup: dependencies,
            clock: new FixedClock(),
            tokenService: dependencies,
            userRoleService: dependencies,
            unitOfWork: dbContext,
            operations: new OtpOperationScopeFactory(dbContext));
    }

    private static HmacOtpCodeHasher CreateHasher()
    {
        return new HmacOtpCodeHasher(
            new OtpOptions
            {
                HashingKey = new string('k', 32)
            });
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => TestNow;
    }

    private sealed class AccountDependencies
        : IIdentityUserLookup,
          IRegistrationAccountService,
          IUserRoleService,
          ITokenService
    {
        public int UnexpectedCalls { get; private set; }

        public Task<bool> PhoneExistsAsync(
            string phoneNumber,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<Guid> CreateAsync(
            string phoneNumber,
            string fullName,
            PreferredLanguage preferredLanguage,
            CancellationToken cancellationToken = default)
        {
            UnexpectedCalls++;

            throw new InvalidOperationException(
                "An account must not be created after failed OTP verification.");
        }

        public Task AssignDefaultRoleAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            UnexpectedCalls++;

            throw new InvalidOperationException(
                "A role must not be assigned after failed OTP verification.");
        }

        public Task<AuthTokenResult> CreateAsync(
            Guid userId,
            string phoneNumber,
            CancellationToken cancellationToken = default)
        {
            UnexpectedCalls++;

            throw new InvalidOperationException(
                "Tokens must not be created after failed OTP verification.");
        }
    }
}
