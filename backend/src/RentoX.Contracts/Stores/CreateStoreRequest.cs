namespace RentoX.Contracts.Stores;

public sealed record CreateStoreRequest(
    string Name,
    string Description,
    string PhoneNumber,
    string? Email,
    string? Address,
    string? InstagramUrl,
    string? TiktokUrl,
    string? FacebookUrl,
    string? WebsiteUrl);