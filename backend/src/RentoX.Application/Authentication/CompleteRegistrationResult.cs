namespace RentoX.Application.Authentication;

public sealed record CompleteRegistrationResult(
    Guid UserId,
    string PhoneNumber,
    AuthTokenResult Tokens);