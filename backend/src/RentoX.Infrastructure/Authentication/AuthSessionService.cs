using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RentoX.Application.Authentication;
using RentoX.Domain.Authentication;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Authentication;

public sealed class AuthSessionService(
    RentoXDbContext dbContext)
    : IAuthSessionService
{
    public async Task<bool> RevokeAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return false;
        }

        string tokenHash = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(refreshToken)));

        RefreshToken? storedToken =
            await dbContext.RefreshTokens
                .SingleOrDefaultAsync(
                    token => token.TokenHash == tokenHash,
                    cancellationToken);

        if (storedToken is null ||
            storedToken.RevokedAtUtc is not null)
        {
            return false;
        }

        storedToken.Revoke(
            DateTimeOffset.UtcNow,
            null);

        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task RevokeAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset utcNow = DateTimeOffset.UtcNow;

        List<RefreshToken> activeTokens =
            await dbContext.RefreshTokens
                .Where(token =>
                    token.UserId == userId &&
                    token.RevokedAtUtc == null &&
                    token.ExpiresAtUtc > utcNow)
                .ToListAsync(cancellationToken);

        foreach (RefreshToken token in activeTokens)
        {
            token.Revoke(utcNow, null);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}