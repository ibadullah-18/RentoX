using RentoX.Application.Abstractions.Time;
using RentoX.Domain.Authentication;
using RentoX.Domain.Authentication.Enums;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Users.Enums;

namespace RentoX.Application.Authentication;

public sealed class CompleteRegistrationService(
    IOtpChallengeRepository challengeRepository,
    IOtpCodeHasher codeHasher,
    IRegistrationAccountService accountService,
    IIdentityUserLookup identityUserLookup,
    IClock clock,
    ITokenService tokenService)
{
    public async Task<CompleteRegistrationResult> CompleteAsync(
        Guid challengeId,
        string code,
        string fullName,
        int preferredLanguage,
        CancellationToken cancellationToken = default)
    {
        OtpChallenge challenge =
            await challengeRepository.GetByIdAsync(
                challengeId,
                cancellationToken)
            ?? throw new DomainException(
                "OTP challenge was not found.");

        if (challenge.Purpose != OtpPurpose.Registration)
        {
            throw new DomainException(
                "OTP challenge is not valid for registration.");
        }

        if (!Enum.IsDefined(
                typeof(PreferredLanguage),
                preferredLanguage))
        {
            throw new DomainException(
                "Preferred language is invalid.");
        }

        bool phoneExists =
            await identityUserLookup.PhoneExistsAsync(
                challenge.PhoneNumber,
                cancellationToken);

        if (phoneExists)
        {
            throw new DomainException(
                "This phone number is already registered.");
        }

        string candidateHash = codeHasher.Hash(
            challenge.PhoneNumber,
            code);

        OtpVerificationResult verificationResult =
            challenge.Verify(
                candidateHash,
                clock.UtcNow);

        if (verificationResult !=
            OtpVerificationResult.Verified)
        {
            throw new DomainException(
                GetVerificationError(verificationResult));
        }

        Guid userId = await accountService.CreateAsync(
            challenge.PhoneNumber,
            fullName,
            (PreferredLanguage)preferredLanguage,
            cancellationToken);

        AuthTokenResult tokens =
            await tokenService.CreateAsync(
        userId,
        challenge.PhoneNumber,
        cancellationToken);

        return new CompleteRegistrationResult(
            userId,
            challenge.PhoneNumber,
            tokens);
    }

    private static string GetVerificationError(
        OtpVerificationResult result)
    {
        return result switch
        {
            OtpVerificationResult.InvalidCode =>
                "OTP code is incorrect.",

            OtpVerificationResult.Expired =>
                "OTP code has expired.",

            OtpVerificationResult.TooManyAttempts =>
                "OTP attempt limit has been exceeded.",

            OtpVerificationResult.AlreadyUsed =>
                "OTP code has already been used.",

            _ => "OTP verification failed."
        };
    }
}