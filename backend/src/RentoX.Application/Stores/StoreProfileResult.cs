namespace RentoX.Application.Stores;

public sealed record StoreProfileResult(
    Guid Id,
    Guid OwnerId,
    string Name,
    string Slug,
    string Description,
    string PhoneNumber,
    string? Email,
    string? Address,
    string? LogoImageKey,
    string? CoverImageKey,
    string? InstagramUrl,
    string? TiktokUrl,
    string? FacebookUrl,
    string? WebsiteUrl,
    int Status,
    string? RejectionReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);