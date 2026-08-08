namespace RentoX.Contracts.Accounts;

public sealed record CurrentUserResponse(
    Guid UserId,
    string PhoneNumber,
    string FullName,
    string? Bio,
    int PreferredLanguage,
    int Status);