using RentoX.Domain.Authentication;
using RentoX.Domain.Authentication.Enums;

namespace RentoX.Application.Authentication;

public interface IOtpChallengeRepository
{
    Task<OtpChallenge?> GetLatestAsync(
        string phoneNumber,
        OtpPurpose purpose,
        CancellationToken cancellationToken = default);

    Task<OtpChallenge?> GetByIdAsync(
        Guid challengeId,
        CancellationToken cancellationToken = default);

    void Add(OtpChallenge challenge);
}