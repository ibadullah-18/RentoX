namespace RentoX.Contracts.Listings;

public sealed record ModerationListingDetailsResponse(
    Guid Id,
    Guid OwnerId,
    string OwnerFullName,
    string PhoneNumber,
    Guid CategoryId,
    string Title,
    string Description,
    decimal Price,
    string Currency,
    int RentalPeriodUnit,
    int Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    IReadOnlyList<ListingImageItemResponse> Images,
    IReadOnlyList<ListingFieldValueDetailsResponse> Fields);