namespace RentoX.Contracts.Listings;

public sealed record UpdateListingRequest(
    string Title,
    string Description,
    decimal Price,
    string Currency,
    int RentalPeriodUnit);