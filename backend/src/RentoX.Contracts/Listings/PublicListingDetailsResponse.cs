namespace RentoX.Contracts.Listings;

public sealed record PublicListingDetailsResponse(
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
    PublicListingOwnerResponse Owner,
    List<ListingImageItemResponse> Images,
    List<ListingFieldValueDetailsResponse> Fields)
{
    public bool IsVip { get; init; }
}