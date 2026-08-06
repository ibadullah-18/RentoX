using Microsoft.EntityFrameworkCore;
using RentoX.Application.Authentication;
using RentoX.Domain.Authentication;
using RentoX.Domain.Authentication.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Authentication;

public sealed class OtpChallengeRepository(
    RentoXDbContext dbContext)
    : IOtpChallengeRepository
{
    public Task<OtpChallenge?> GetLatestAsync(
        string phoneNumber,
        OtpPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        return dbContext.OtpChallenges
            .Where(challenge =>
                challenge.PhoneNumber == phoneNumber
                && challenge.Purpose == purpose)
            .OrderByDescending(challenge =>
                challenge.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<OtpChallenge?> GetByIdAsync(
    Guid challengeId,
    CancellationToken cancellationToken = default)
    {
        return dbContext.OtpChallenges.SingleOrDefaultAsync(
            challenge => challenge.Id == challengeId,
            cancellationToken);
    }

    public void Add(OtpChallenge challenge)
    {
        ArgumentNullException.ThrowIfNull(challenge);

        dbContext.OtpChallenges.Add(challenge);
    }
}