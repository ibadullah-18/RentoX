namespace RentoX.Application.Listings;

public sealed record UpdateListingFieldsResult(
    Guid ListingId,
    int FieldCount,
    int Status,
    DateTimeOffset? UpdatedAtUtc);