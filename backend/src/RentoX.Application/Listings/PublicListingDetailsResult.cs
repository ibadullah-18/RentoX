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
    List<ListingFieldValueDetailsResult> Fields)
{
    public bool IsVip { get; init; }

    public Guid? CityId { get; init; }

    public string? CityName { get; init; }

    public Guid? DistrictId { get; init; }

    public string? DistrictName { get; init; }
}