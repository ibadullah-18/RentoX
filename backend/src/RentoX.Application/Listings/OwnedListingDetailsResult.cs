namespace RentoX.Application.Listings;

public sealed record OwnedListingDetailsResult(
    Guid Id,
    Guid OwnerId,
    Guid CategoryId,
    string Title,
    string Description,
    decimal Price,
    string Currency,
    int RentalPeriodUnit,
    int Status,
    string? RejectionReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    DateTimeOffset? PublishedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    IReadOnlyList<ListingImageItemResult> Images,
    IReadOnlyList<ListingFieldValueDetailsResult> Fields)
{
    public Guid? CityId { get; init; }

    public string? CityName { get; init; }

    public Guid? DistrictId { get; init; }

    public string? DistrictName { get; init; }
}