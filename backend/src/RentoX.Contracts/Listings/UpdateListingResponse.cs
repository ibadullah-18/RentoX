namespace RentoX.Contracts.Listings;

public sealed record UpdateListingResponse(
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