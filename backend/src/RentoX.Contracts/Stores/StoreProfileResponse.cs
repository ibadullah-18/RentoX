namespace RentoX.Contracts.Stores;

public sealed record StoreProfileResponse(
    Guid Id,
    Guid OwnerId,
    string Name,
    string Slug,
    string Description,
    string PhoneNumber,
    string? Email,
    string? Address,
    string? LogoImageUrl,
    string? CoverImageUrl,
    string? InstagramUrl,
    string? TiktokUrl,
    string? FacebookUrl,
    string? WebsiteUrl,
    int Status,
    string? RejectionReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);