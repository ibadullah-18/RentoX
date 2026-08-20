namespace RentoX.Contracts.Listings;

public sealed record UpdateListingFieldsResponse(
    Guid ListingId,
    int FieldCount,
    int Status,
    DateTimeOffset? UpdatedAtUtc);