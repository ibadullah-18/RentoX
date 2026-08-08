using RentoX.Application.Abstractions.Persistence;
using RentoX.Application.Abstractions.Time;
using RentoX.Domain.Authentication;
using RentoX.Domain.Authentication.Enums;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Application.Authentication;

public sealed class CompleteLoginService(
    IOtpChallengeRepository challengeRepository,
    IOtpCodeHasher codeHasher,
    ILoginAccountService loginAccountService,
    IClock clock,
    IUnitOfWork unitOfWork,
    ITokenService tokenService)
{
    public async Task<CompleteLoginResult> CompleteAsync(
        Guid challengeId,
        string code,
        CancellationToken cancellationToken = default)
    {
        OtpChallenge challenge =
            await challengeRepository.GetByIdAsync(
                challengeId,
                cancellationToken)
            ?? throw new DomainException(
                "OTP challenge was not found.");

        if (challenge.Purpose != OtpPurpose.Login)
        {
            throw new DomainException(
                "OTP challenge is not valid for login.");
        }

        string candidateHash = codeHasher.Hash(
            challenge.PhoneNumber,
            code);

        OtpVerificationResult verificationResult =
            challenge.Verify(
                candidateHash,
                clock.UtcNow);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        if (verificationResult !=
            OtpVerificationResult.Verified)
        {
            throw new DomainException(
                GetVerificationError(verificationResult));
        }

        Guid? userId =
            await loginAccountService.FindUserIdAsync(
                challenge.PhoneNumber,
                cancellationToken);

        if (!userId.HasValue)
        {
            throw new DomainException(
                "User account was not found.");
        }

        AuthTokenResult tokens =
            await tokenService.CreateAsync(
                userId.Value,
                challenge.PhoneNumber,
                cancellationToken);

        return new CompleteLoginResult(
            userId.Value,
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