namespace RentoX.Application.Stores;

public sealed record PublicStoreDetailsResult(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? PhoneNumber,
    string? Email,
    string? Address,
    string? InstagramUrl,
    string? TiktokUrl,
    string? FacebookUrl,
    string? WebsiteUrl,
    bool HasLogoImage,
    bool HasCoverImage,
    long ImageVersion,
    long ActiveListingCount,
    long TotalViewCount,
    long TotalFavoriteCount,
    DateTimeOffset CreatedAtUtc);