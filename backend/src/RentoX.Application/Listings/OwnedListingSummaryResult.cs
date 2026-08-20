namespace RentoX.Application.Listings;

public sealed record OwnedListingSummaryResult(
    Guid Id,
    Guid CategoryId,
    string Title,
    decimal Price,
    string Currency,
    int RentalPeriodUnit,
    int Status,
    Guid? CoverImageId,
    int ImageCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? PublishedAtUtc,
    DateTimeOffset? ExpiresAtUtc);