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

public sealed class SupersededOtpTests(
    AuthenticationPostgresFixture fixture)
    : IClassFixture<AuthenticationPostgresFixture>
{
    private static readonly DateTimeOffset TestNow =
        new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(OtpPurpose.Registration)]
    [InlineData(OtpPurpose.Login)]
    public async Task OlderChallengeIsRejectedEvenWithCorrectCode(
        OtpPurpose purpose)
    {
        PhoneNumber phone =
            PhoneNumber.Create("+994500000003");

        HmacOtpCodeHasher hasher = new(
            new OtpOptions
            {
                HashingKey = new string('k', 32)
            });

        OtpChallenge older = OtpChallenge.Create(
            phone,
            hasher.Hash(phone.Value, "123456"),
            purpose,
            TestNow.AddMinutes(-2),
            TimeSpan.FromMinutes(5),
            maximumAttempts: 5);

        OtpChallenge newer = OtpChallenge.Create(
            phone,
            hasher.Hash(phone.Value, "654321"),
            purpose,
            TestNow.AddMinutes(-1),
            TimeSpan.FromMinutes(5),
            maximumAttempts: 5);

        await using (RentoXDbContext seedContext =
                     fixture.CreateContext())
        {
            seedContext.OtpChallenges.Add(older);
            seedContext.OtpChallenges.Add(newer);

            await seedContext.SaveChangesAsync();
        }

        AccountDependencies dependencies = new();

        await using (RentoXDbContext requestContext =
                     fixture.CreateContext())
        {
            OtpChallengeRepository repository = new(requestContext);
            DomainException exception;

            if (purpose == OtpPurpose.Registration)
            {
                CompleteRegistrationService service = new(
                    challengeRepository: repository,
                    codeHasher: hasher,
                    accountService: dependencies,
                    identityUserLookup: dependencies,
                    clock: new FixedClock(),
                    tokenService: dependencies,
                    userRoleService: dependencies,
                    unitOfWork: requestContext,
                    operations: new OtpOperationScopeFactory(requestContext));

                exception = await Assert.ThrowsAsync<DomainException>(
                    () => service.CompleteAsync(
                        older.Id,
                        "123456",
                        "OTP Test User",
                        (int)PreferredLanguage.Azerbaijani));
            }
            else
            {
                CompleteLoginService service = new(
                    challengeRepository: repository,
                    codeHasher: hasher,
                    loginAccountService: dependencies,
                    clock: new FixedClock(),
                    unitOfWork: requestContext,
                    tokenService: dependencies,
                    operations: new OtpOperationScopeFactory(requestContext));

                exception = await Assert.ThrowsAsync<DomainException>(
                    () => service.CompleteAsync(
                        older.Id,
                        "123456"));
            }

            Assert.Equal(
                "A newer OTP code has been requested. Please use the latest code.",
                exception.Message);
        }

        await using RentoXDbContext readContext =
            fixture.CreateContext();

        OtpChallenge savedOlder =
            await readContext.OtpChallenges
                .AsNoTracking()
                .SingleAsync(item => item.Id == older.Id);

        OtpChallenge savedNewer =
            await readContext.OtpChallenges
                .AsNoTracking()
                .SingleAsync(item => item.Id == newer.Id);

        Assert.Null(savedOlder.VerifiedAtUtc);
        Assert.Null(savedNewer.VerifiedAtUtc);
        Assert.Equal(0, savedOlder.FailedAttemptCount);
        Assert.Equal(0, savedNewer.FailedAttemptCount);
        Assert.Equal(0, dependencies.UnexpectedCalls);
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => TestNow;
    }

    private sealed class AccountDependencies
        : IIdentityUserLookup,
          IRegistrationAccountService,
          ILoginAccountService,
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

        public Task<Guid?> FindUserIdAsync(
            string phoneNumber,
            CancellationToken cancellationToken = default)
        {
            throw UnexpectedCall();
        }

        public Task<Guid> CreateAsync(
            string phoneNumber,
            string fullName,
            PreferredLanguage preferredLanguage,
            CancellationToken cancellationToken = default)
        {
            throw UnexpectedCall();
        }

        public Task AssignDefaultRoleAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            throw UnexpectedCall();
        }

        public Task<AuthTokenResult> CreateAsync(
            Guid userId,
            string phoneNumber,
            CancellationToken cancellationToken = default)
        {
            throw UnexpectedCall();
        }

        private InvalidOperationException UnexpectedCall()
        {
            UnexpectedCalls++;

            return new InvalidOperationException(
                "A superseded OTP must not reach account or token operations.");
        }
    }
}
