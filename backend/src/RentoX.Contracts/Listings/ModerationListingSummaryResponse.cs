namespace RentoX.Contracts.Listings;

public sealed record ModerationListingSummaryResponse(
    Guid Id,
    Guid OwnerId,
    Guid CategoryId,
    string Title,
    decimal Price,
    string Currency,
    int RentalPeriodUnit,
    string? CoverImageUrl,
    int ImageCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);  