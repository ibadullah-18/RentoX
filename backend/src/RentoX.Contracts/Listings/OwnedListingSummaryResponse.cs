namespace RentoX.Contracts.Listings;

public sealed record OwnedListingSummaryResponse(
    Guid Id,
    Guid CategoryId,
    string Title,
    decimal Price,
    string Currency,
    int RentalPeriodUnit,
    int Status,
    string? CoverImageUrl,
    int ImageCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? PublishedAtUtc,
    DateTimeOffset? ExpiresAtUtc);