using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Authentication;
using RentoX.Domain.Authentication;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Authentication;

public sealed class RefreshTokenService(
    RentoXDbContext dbContext,
    ITokenService tokenService,
    IClock clock)
    : IRefreshTokenService
{
    public async Task<AuthTokenResult?> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        string tokenHash = HashToken(refreshToken);

        Guid? ownerId = await dbContext.RefreshTokens
            .AsNoTracking()
            .Where(token => token.TokenHash == tokenHash)
            .Select(token => (Guid?)token.UserId)
            .SingleOrDefaultAsync(cancellationToken);

        if (!ownerId.HasValue)
        {
            return null;
        }

        await using AuthSessionTransaction operation =
            await AuthSessionTransaction.BeginAsync(
                dbContext,
                ownerId.Value,
                cancellationToken);

        RefreshToken? storedToken =
            await dbContext.RefreshTokens.SingleOrDefaultAsync(
                token =>
                    token.TokenHash == tokenHash &&
                    token.UserId == ownerId.Value,
                cancellationToken);

        DateTimeOffset utcNow = clock.UtcNow;

        if (storedToken is null ||
            storedToken.RevokedAtUtc is not null ||
            storedToken.ExpiresAtUtc <= utcNow)
        {
            return null;
        }

        string? phoneNumber = await dbContext.Users
            .Where(user => user.Id == ownerId.Value)
            .Select(user => user.PhoneNumber)
            .SingleOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        AuthTokenResult newTokens =
            await tokenService.CreateAsync(
                ownerId.Value,
                phoneNumber,
                cancellationToken);

        string replacementHash = HashToken(newTokens.RefreshToken);

        Guid replacementId = await dbContext.RefreshTokens
            .AsNoTracking()
            .Where(token =>
                token.UserId == ownerId.Value &&
                token.TokenHash == replacementHash)
            .Select(token => token.Id)
            .SingleAsync(cancellationToken);

        if (replacementId == storedToken.Id)
        {
            throw new InvalidOperationException(
                "Refresh token rotation did not create a new token.");
        }

        storedToken.Revoke(utcNow, replacementId);

        await dbContext.SaveChangesAsync(cancellationToken);
        await operation.CommitAsync(cancellationToken);

        return newTokens;
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
