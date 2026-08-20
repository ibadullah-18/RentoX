namespace RentoX.Application.Listings;

public sealed record ListingImageItemResult(
    Guid Id,
    int DisplayOrder,
    bool IsCover);