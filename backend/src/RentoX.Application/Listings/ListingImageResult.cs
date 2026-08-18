namespace RentoX.Application.Listings;

public sealed record ListingImageResult(
    Guid Id,
    Guid ListingId,
    string StorageKey,
    string ContentType,
    long SizeBytes,
    int DisplayOrder,
    bool IsCover);