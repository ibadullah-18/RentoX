namespace RentoX.Application.Stores;

public sealed record FollowedStoreResult(
    Guid StoreId,
    string Name,
    string Slug,
    bool HasLogoImage,
    long ImageVersion,
    int ActiveListingCount,
    int FollowerCount,
    DateTimeOffset FollowedAtUtc);