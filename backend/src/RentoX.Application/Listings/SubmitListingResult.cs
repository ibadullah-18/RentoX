namespace RentoX.Application.Listings;

public sealed record SubmitListingResult(
    Guid ListingId,
    int Status,
    DateTimeOffset? UpdatedAtUtc);