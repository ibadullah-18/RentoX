namespace RentoX.Application.Listings;

public sealed record ModerationListingSummaryResult(
    Guid Id,
    Guid OwnerId,
    Guid CategoryId,
    string Title,
    decimal Price,
    string Currency,
    int RentalPeriodUnit,
    Guid? CoverImageId,
    int ImageCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);