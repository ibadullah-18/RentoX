namespace RentoX.Contracts.Listings;

public sealed record PublicListingSummaryResponse(
    Guid Id,
    Guid OwnerId,
    Guid CategoryId,
    string CategoryName,
    string Title,
    decimal Price,
    string Currency,
    int RentalPeriodUnit,
    string? CoverImageUrl,
    long ViewCount,
    int FavoriteCount,
    bool IsFavorite,
    DateTimeOffset PublishedAtUtc,
    DateTimeOffset ExpiresAtUtc)
{
    public bool IsVip { get; init; }
}