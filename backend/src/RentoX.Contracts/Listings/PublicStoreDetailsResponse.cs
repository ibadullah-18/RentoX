namespace RentoX.Contracts.Stores;

public sealed record PublicStoreDetailsResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? PhoneNumber,
    string? Email,
    string? Address,
    string? LogoImageUrl,
    string? CoverImageUrl,
    string? InstagramUrl,
    string? TiktokUrl,
    string? FacebookUrl,
    string? WebsiteUrl,
    long ActiveListingCount,
    long TotalViewCount,
    long TotalFavoriteCount,
    DateTimeOffset CreatedAtUtc,
    string? YoutubeUrl = null,
    string? WhatsAppNumber = null,
    string? WhatsAppMessage = null,
    IReadOnlyList<RentoX.Contracts.Stores.StoreHoursDayDto>? WorkingHours = null,
    long FollowerCount = 0);