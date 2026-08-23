namespace RentoX.Contracts.Listings;

public sealed record SubmitListingResponse(
    Guid ListingId,
    int Status,
    DateTimeOffset? UpdatedAtUtc);