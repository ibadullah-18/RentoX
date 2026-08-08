namespace RentoX.Contracts.Accounts;

public sealed record UpdateAccountProfileRequest(
    string FullName,
    string? Bio,
    int PreferredLanguage);