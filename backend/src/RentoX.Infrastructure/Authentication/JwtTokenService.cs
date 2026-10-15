using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Authentication;
using RentoX.Domain.Authentication;
using Microsoft.EntityFrameworkCore;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Persistence;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace RentoX.Infrastructure.Authentication;

public sealed class JwtTokenService(
    JwtOptions options,
    IClock clock,
    RentoXDbContext dbContext,
    UserManager<AppUser> userManager)
    : ITokenService
{
    public async Task<AuthTokenResult> CreateAsync(
        Guid userId,
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        ValidateOptions();

        await using AuthSessionTransaction operation =
            await AuthSessionTransaction.BeginAsync(
                dbContext,
                userId,
                cancellationToken);

        DateTimeOffset now = clock.UtcNow;

        DateTimeOffset accessTokenExpiresAt =
            now.Add(options.AccessTokenLifetime);

        string userIdText = userId.ToString(
            "D",
            CultureInfo.InvariantCulture);

        AppUser? user =
            await userManager.FindByIdAsync(userIdText);

        if (user is null)
        {
            throw new InvalidOperationException(
                "User account was not found.");
        }

        UserStatus? status = await dbContext.Set<UserProfile>()
            .AsNoTracking()
            .Where(p => p.Id == userId)
            .Select(p => (UserStatus?)p.Status)
            .SingleOrDefaultAsync(cancellationToken);

        if (status is UserStatus.Blocked or UserStatus.Suspended)
        {
            throw new DomainException("This account is blocked.");
        }

        IList<string> roles =
            await userManager.GetRolesAsync(user);

        List<Claim> claims =
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

        // An Admin limited to some sections of the panel: they ride in the token.
        if (roles.Contains(RentoX.Application.Authorization.RoleNames.Admin))
        {
            foreach (Claim section in (await userManager.GetClaimsAsync(user))
                         .Where(c => c.Type == RentoX.Application.Authorization.AdminSections.ClaimType))
            {
                claims.Add(new Claim(RentoX.Application.Authorization.AdminSections.ClaimType, section.Value));
            }
        }

        claims.AddRange(
            roles.Select(role =>
                new Claim(
                    ClaimTypes.Role,
                    role)));

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

        await operation.CommitAsync(cancellationToken);

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
