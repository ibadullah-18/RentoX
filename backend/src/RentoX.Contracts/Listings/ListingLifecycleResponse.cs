namespace RentoX.Contracts.Listings;

public sealed record ListingLifecycleResponse(
    Guid ListingId,
    int Status,
    DateTimeOffset? PublishedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? DeletedAtUtc,
    DateTimeOffset? UpdatedAtUtc);