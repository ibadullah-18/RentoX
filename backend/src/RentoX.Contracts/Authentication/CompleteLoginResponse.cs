namespace RentoX.Contracts.Authentication;

public sealed record CompleteLoginResponse(
    Guid UserId,
    string PhoneNumber,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    DateTimeOffset RefreshTokenExpiresAtUtc);