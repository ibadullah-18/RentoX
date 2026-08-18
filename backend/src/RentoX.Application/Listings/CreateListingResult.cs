namespace RentoX.Application.Listings;

public sealed record CreateListingResult(
    Guid Id,
    Guid OwnerId,
    Guid CategoryId,
    string Title,
    decimal Price,
    string Currency,
    int RentalPeriodUnit,
    int Status,
    DateTimeOffset CreatedAtUtc);