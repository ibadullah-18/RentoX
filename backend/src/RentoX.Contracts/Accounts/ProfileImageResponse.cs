namespace RentoX.Contracts.Accounts;

public sealed record ProfileImageResponse(
    Guid UserId,
    string Url);