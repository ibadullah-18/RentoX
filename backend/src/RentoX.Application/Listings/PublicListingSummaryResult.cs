namespace RentoX.Application.Listings;

public sealed record PublicListingSummaryResult(
    Guid Id,
    Guid OwnerId,
    Guid CategoryId,
    string CategoryName,
    string Title,
    decimal Price,
    string Currency,
    int RentalPeriodUnit,
    Guid? CoverImageId,
    long ViewCount,
    int FavoriteCount,
    bool IsFavorite,
    DateTimeOffset PublishedAtUtc,
    DateTimeOffset ExpiresAtUtc)
{
    public bool IsVip { get; init; }

    /// <summary>"City, district" for the card.</summary>
    public string? Location { get; init; }
}