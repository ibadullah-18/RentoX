namespace RentoX.Application.Accounts;

public sealed record AccountProfileResult(
    Guid UserId,
    string PhoneNumber,
    string FullName,
    string? Bio,
    int PreferredLanguage,
    int Status);