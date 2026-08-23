namespace RentoX.Contracts.Listings;

public sealed record ListingModerationResponse(
    Guid ListingId,
    int Status,
    string? RejectionReason,
    DateTimeOffset? PublishedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? UpdatedAtUtc);