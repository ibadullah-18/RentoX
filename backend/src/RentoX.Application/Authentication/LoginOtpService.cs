using RentoX.Application.Abstractions.Persistence;
using RentoX.Application.Abstractions.Time;
using RentoX.Domain.Authentication;
using RentoX.Domain.Authentication.Enums;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Application.Authentication;

public sealed class LoginOtpService(
    IOtpChallengeRepository challengeRepository,
    IOtpCodeGenerator codeGenerator,
    IOtpCodeHasher codeHasher,
    ISmsSender smsSender,
    IIdentityUserLookup identityUserLookup,
    IOtpPolicy policy,
    IClock clock,
    IUnitOfWork unitOfWork)
{
    public async Task<LoginOtpResult> RequestAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        PhoneNumber normalizedPhone =
            PhoneNumber.Create(phoneNumber);

        bool phoneExists =
            await identityUserLookup.PhoneExistsAsync(
                normalizedPhone.Value,
                cancellationToken);

        if (!phoneExists)
        {
            throw new DomainException(
                "An account with this phone number was not found.");
        }

        DateTimeOffset now = clock.UtcNow;

        OtpChallenge? latest =
            await challengeRepository.GetLatestAsync(
                normalizedPhone.Value,
                OtpPurpose.Login,
                cancellationToken);

        if (latest is not null)
        {
            DateTimeOffset resendAvailableAt =
                latest.CreatedAtUtc.Add(
                    policy.ResendInterval);

            if (resendAvailableAt > now)
            {
                throw new DomainException(
                    "Please wait before requesting another code.");
            }
        }

        string code = codeGenerator.Generate();

        string codeHash = codeHasher.Hash(
            normalizedPhone.Value,
            code);

        OtpChallenge challenge = OtpChallenge.Create(
            normalizedPhone,
            codeHash,
            OtpPurpose.Login,
            now,
            policy.Lifetime,
            policy.MaximumAttempts);

        challengeRepository.Add(challenge);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        await smsSender.SendOtpAsync(
            normalizedPhone.Value,
            code,
            cancellationToken);

        return new LoginOtpResult(
            challenge.Id,
            challenge.ExpiresAtUtc,
            now.Add(policy.ResendInterval));
    }
}