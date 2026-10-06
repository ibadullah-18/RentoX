using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Authentication;
using RentoX.Domain.Authentication;
using RentoX.Domain.Common.Exceptions;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Authentication;

public sealed class AuthSessionService(
    RentoXDbContext dbContext,
    IClock clock,
    ICurrentUserContext currentUserContext)
    : IAuthSessionService
{
    public async Task<bool> RevokeAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        Guid? currentUserId = currentUserContext.UserId;

        if (!currentUserContext.IsAuthenticated ||
            !currentUserId.HasValue ||
            currentUserId.Value == Guid.Empty ||
            string.IsNullOrWhiteSpace(refreshToken))
        {
            return false;
        }

        Guid userId = currentUserId.Value;

        string tokenHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

        await using AuthSessionTransaction operation =
            await AuthSessionTransaction.BeginAsync(
                dbContext,
                userId,
                cancellationToken);

        RefreshToken? token =
            await dbContext.RefreshTokens.SingleOrDefaultAsync(
                item =>
                    item.UserId == userId &&
                    item.TokenHash == tokenHash,
                cancellationToken);

        if (token is null)
        {
            return false;
        }

        DateTimeOffset utcNow = clock.UtcNow;
        HashSet<Guid> visited = [];

        while (token is not null)
        {
            if (!visited.Add(token.Id))
            {
                throw new InvalidOperationException(
                    "Refresh token replacement chain contains a cycle.");
            }

            Guid? replacementId = token.ReplacedByTokenId;

            token.Revoke(utcNow, replacementId);

            if (!replacementId.HasValue)
            {
                break;
            }

            token = await dbContext.RefreshTokens.SingleOrDefaultAsync(
                item =>
                    item.Id == replacementId.Value &&
                    item.UserId == userId,
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await operation.CommitAsync(cancellationToken);

        return true;
    }

    public async Task RevokeAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (!currentUserContext.IsAuthenticated ||
            userId == Guid.Empty ||
            currentUserContext.UserId != userId)
        {
            throw new DomainException(
                "You can only revoke your own sessions.");
        }

        await using AuthSessionTransaction operation =
            await AuthSessionTransaction.BeginAsync(
                dbContext,
                userId,
                cancellationToken);

        DateTimeOffset utcNow = clock.UtcNow;

        List<RefreshToken> tokens = await dbContext.RefreshTokens
            .Where(token =>
                token.UserId == userId &&
                token.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (RefreshToken token in tokens)
        {
            token.Revoke(utcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await operation.CommitAsync(cancellationToken);
    }
}
