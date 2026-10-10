namespace RentoX.Application.Stores;

public sealed record PublicStoreSummaryResult(
    Guid StoreId,
    string Name,
    string Slug,
    string? Description,
    bool HasLogoImage,
    long ImageVersion,
    int ActiveListingCount,
    int FollowerCount);
