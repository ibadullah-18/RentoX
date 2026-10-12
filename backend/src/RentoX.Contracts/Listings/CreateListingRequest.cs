namespace RentoX.Contracts.Listings;

public sealed record CreateListingRequest(
    Guid CategoryId,
    string Title,
    string Description,
    decimal Price,
    string Currency,
    int RentalPeriodUnit,
    IReadOnlyList<CreateListingFieldRequest> Fields,
    Guid? CityId = null,
    Guid? DistrictId = null);