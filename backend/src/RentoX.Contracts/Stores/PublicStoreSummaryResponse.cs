namespace RentoX.Contracts.Stores;

public sealed record PublicStoreSummaryResponse(
    Guid StoreId,
    string Name,
    string Slug,
    string? Description,
    string? LogoImageUrl,
    int ActiveListingCount,
    int FollowerCount);
