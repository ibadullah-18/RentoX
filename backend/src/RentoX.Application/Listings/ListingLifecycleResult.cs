namespace RentoX.Application.Listings;

public sealed record ListingLifecycleResult(
    Guid ListingId,
    int Status,
    DateTimeOffset? PublishedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? DeletedAtUtc,
    DateTimeOffset? UpdatedAtUtc);