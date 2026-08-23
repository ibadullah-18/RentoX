namespace RentoX.Application.Stores;

public sealed record CreateStoreCommand(
    Guid OwnerId,
    string Name,
    string Description,
    string PhoneNumber,
    string? Email,
    string? Address,
    string? InstagramUrl,
    string? TiktokUrl,
    string? FacebookUrl,
    string? WebsiteUrl);