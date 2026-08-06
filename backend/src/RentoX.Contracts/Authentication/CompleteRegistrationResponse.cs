namespace RentoX.Contracts.Authentication;

public sealed record CompleteRegistrationResponse(
    Guid UserId,
    string PhoneNumber,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    DateTimeOffset RefreshTokenExpiresAtUtc);