namespace RentoX.Contracts.Locations;

public sealed record CityResponse(Guid Id, string Name, bool HasDistricts);

public sealed record DistrictResponse(Guid Id, string Name);
