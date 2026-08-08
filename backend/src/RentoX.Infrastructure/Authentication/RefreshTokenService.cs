using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RentoX.Application.Authentication;
using RentoX.Domain.Authentication;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Authentication;

public sealed class RefreshTokenService(
    RentoXDbContext dbContext,
    ITokenService tokenService)
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

        string tokenHash = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(refreshToken)));

        RefreshToken? storedToken =
            await dbContext.RefreshTokens
                .SingleOrDefaultAsync(
                    token => token.TokenHash == tokenHash,
                    cancellationToken);

        DateTimeOffset utcNow = DateTimeOffset.UtcNow;

        if (storedToken is null ||
            storedToken.RevokedAtUtc is not null ||
            storedToken.ExpiresAtUtc <= utcNow)
        {
            return null;
        }

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        storedToken.Revoke(utcNow, null);

        string? phoneNumber =
            await dbContext.Users
                .Where(user => user.Id == storedToken.UserId)
                .Select(user => user.PhoneNumber)
                .SingleOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        AuthTokenResult newTokens =
            await tokenService.CreateAsync(
                storedToken.UserId,
                phoneNumber,
                cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return newTokens;
    }
}