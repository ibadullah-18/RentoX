namespace RentoX.Application.Authentication;

public sealed record CompleteLoginResult(
    Guid UserId,
    string PhoneNumber,
    AuthTokenResult Tokens);