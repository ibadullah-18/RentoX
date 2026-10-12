namespace RentoX.Application.Listings;

public sealed record UpdateListingCommand(
    Guid OwnerId,
    Guid ListingId,
    string Title,
    string Description,
    decimal Price,
    string Currency,
    int RentalPeriodUnit,
    Guid? CityId = null,
    Guid? DistrictId = null);