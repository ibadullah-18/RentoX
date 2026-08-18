namespace RentoX.Contracts.Listings;

public sealed record ListingImageResponse(
    Guid Id,
    Guid ListingId,
    string Url,
    int DisplayOrder,
    bool IsCover,
    long SizeBytes);