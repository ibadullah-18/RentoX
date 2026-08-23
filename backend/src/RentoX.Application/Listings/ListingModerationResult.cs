namespace RentoX.Application.Listings;

public sealed record ListingModerationResult(
    Guid ListingId,
    int Status,
    string? RejectionReason,
    DateTimeOffset? PublishedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? UpdatedAtUtc);