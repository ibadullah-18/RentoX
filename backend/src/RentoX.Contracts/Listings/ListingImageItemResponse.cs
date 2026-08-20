namespace RentoX.Contracts.Listings;

public sealed record ListingImageItemResponse(
    Guid Id,
    string Url,
    int DisplayOrder,
    bool IsCover);