namespace RentoX.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public required string Issuer { get; init; }

    public required string Audience { get; init; }

    public required string SigningKey { get; init; }

    public TimeSpan AccessTokenLifetime { get; init; } =
        TimeSpan.FromMinutes(15);

    public TimeSpan RefreshTokenLifetime { get; init; } =
        TimeSpan.FromDays(30);
}