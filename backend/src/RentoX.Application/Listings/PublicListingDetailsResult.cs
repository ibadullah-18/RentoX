namespace RentoX.Application.Listings;

public sealed record PublicListingDetailsResult(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Title,
    string Description,
    decimal Price,
    string Currency,
    int RentalPeriodUnit,
    long ViewCount,
    int FavoriteCount,
    bool IsFavorite,
    DateTimeOffset PublishedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    PublicListingOwnerResult Owner,
    List<ListingImageItemResult> Images,
    List<ListingFieldValueDetailsResult> Fields);