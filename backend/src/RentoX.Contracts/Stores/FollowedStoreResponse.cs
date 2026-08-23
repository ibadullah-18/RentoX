namespace RentoX.Contracts.Stores;

public sealed record FollowedStoreResponse(
    Guid StoreId,
    string Name,
    string Slug,
    string? LogoImageUrl,
    int ActiveListingCount,
    int FollowerCount,
    DateTimeOffset FollowedAtUtc);