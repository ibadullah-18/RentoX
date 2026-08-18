namespace RentoX.Contracts.Listings;

public sealed record CreateListingResponse(
    Guid Id,
    Guid OwnerId,
    Guid CategoryId,
    string Title,
    decimal Price,
    string Currency,
    int RentalPeriodUnit,
    int Status,
    DateTimeOffset CreatedAtUtc);