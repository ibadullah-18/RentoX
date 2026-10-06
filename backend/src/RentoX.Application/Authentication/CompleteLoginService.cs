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
    ITokenService tokenService,
    IOtpOperationScopeFactory operations)
{
    public async Task<CompleteLoginResult> CompleteAsync(
        Guid challengeId,
        string code,
        CancellationToken cancellationToken = default)
    {
        await using IOtpOperationScope operation =
            await operations.BeginForChallengeAsync(
                challengeId,
                cancellationToken);

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

        OtpChallenge? latest =
            await challengeRepository.GetLatestAsync(
                challenge.PhoneNumber,
                challenge.Purpose,
                cancellationToken);

        if (latest is null || latest.Id != challenge.Id)
        {
            throw new DomainException(
                "A newer OTP code has been requested. Please use the latest code.");
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
            await operation.CommitAsync(cancellationToken);

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

        await operation.CommitAsync(cancellationToken);

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
