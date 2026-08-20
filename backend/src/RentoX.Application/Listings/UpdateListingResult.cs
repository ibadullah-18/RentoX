namespace RentoX.Application.Listings;

public sealed record UpdateListingResult(
    Guid Id,
    Guid OwnerId,
    Guid CategoryId,
    string Title,
    string Description,
    decimal Price,
    string Currency,
    int RentalPeriodUnit,
    int Status,
    DateTimeOffset? UpdatedAtUtc);