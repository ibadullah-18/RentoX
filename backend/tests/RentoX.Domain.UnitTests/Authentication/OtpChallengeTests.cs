using RentoX.Domain.Authentication;
using RentoX.Domain.Authentication.Enums;
using Xunit;

namespace RentoX.Domain.UnitTests.Authentication;

public sealed class OtpChallengeTests
{
    [Fact]
    public void CorrectCodeShouldVerifyChallenge()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        OtpChallenge challenge = OtpChallenge.Create(
            PhoneNumber.Create("0501234567"),
            "CORRECT_HASH",
            OtpPurpose.Registration,
            now,
            TimeSpan.FromMinutes(5),
            5);

        OtpVerificationResult result = challenge.Verify(
            "CORRECT_HASH",
            now.AddSeconds(10));

        Assert.Equal(
            OtpVerificationResult.Verified,
            result);

        Assert.NotNull(challenge.VerifiedAtUtc);
    }

    [Fact]
    public void WrongCodeShouldIncreaseFailedAttempts()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        OtpChallenge challenge = OtpChallenge.Create(
            PhoneNumber.Create("0501234567"),
            "CORRECT_HASH",
            OtpPurpose.Registration,
            now,
            TimeSpan.FromMinutes(5),
            5);

        OtpVerificationResult result = challenge.Verify(
            "WRONG_HASH",
            now.AddSeconds(10));

        Assert.Equal(
            OtpVerificationResult.InvalidCode,
            result);

        Assert.Equal(1, challenge.FailedAttemptCount);
    }
}               