using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Authentication;
using RentoX.Domain.Authentication;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Authentication;

public sealed class JwtTokenService(
    JwtOptions options,
    IClock clock,
    RentoXDbContext dbContext)
    : ITokenService
{
    public async Task<AuthTokenResult> CreateAsync(
        Guid userId,
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        ValidateOptions();

        DateTimeOffset now = clock.UtcNow;

        DateTimeOffset accessTokenExpiresAt =
            now.Add(options.AccessTokenLifetime);

        string userIdText = userId.ToString(
            "D",
            CultureInfo.InvariantCulture);

        Claim[] claims =
        [
            new(
                JwtRegisteredClaimNames.Sub,
                userIdText),

            new(
                ClaimTypes.NameIdentifier,
                userIdText),

            new(
                ClaimTypes.MobilePhone,
                phoneNumber),

            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString(
                    "D",
                    CultureInfo.InvariantCulture))
        ];

        SymmetricSecurityKey securityKey = new(
            Encoding.UTF8.GetBytes(
                options.SigningKey));

        SigningCredentials credentials = new(
            securityKey,
            SecurityAlgorithms.HmacSha256);

        JwtSecurityToken jwt = new(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: accessTokenExpiresAt.UtcDateTime,
            signingCredentials: credentials);

        string accessToken =
            new JwtSecurityTokenHandler()
                .WriteToken(jwt);

        string rawRefreshToken =
            Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(64));

        string refreshTokenHash = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    rawRefreshToken)));

        RefreshToken refreshToken =
            RefreshToken.Create(
                userId,
                refreshTokenHash,
                now,
                options.RefreshTokenLifetime);

        dbContext.RefreshTokens.Add(refreshToken);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return new AuthTokenResult(
            accessToken,
            rawRefreshToken,
            accessTokenExpiresAt,
            refreshToken.ExpiresAtUtc);
    }

    private void ValidateOptions()
    {
        if (options.SigningKey.Length < 64)
        {
            throw new InvalidOperationException(
                "JWT signing key must contain at least 64 characters.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            options.Issuer);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            options.Audience);
    }
}