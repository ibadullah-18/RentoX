namespace RentoX.Application.Listings;

public sealed record ModerationListingDetailsResult(
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
    IReadOnlyList<ListingImageItemResult> Images,
    IReadOnlyList<ListingFieldValueDetailsResult> Fields);