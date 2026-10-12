using RentoX.Domain.Users.Enums;

namespace RentoX.Application.Locations;

public sealed record CityResult(Guid Id, string Name, bool HasDistricts);

public sealed record DistrictResult(Guid Id, string Name);

public interface ILocationQueryService
{
    Task<IReadOnlyList<CityResult>> GetCitiesAsync(
        PreferredLanguage language,
        CancellationToken cancellationToken = default);

    /// <summary>The districts of a city, or <c>null</c> when it is unknown.</summary>
    Task<IReadOnlyList<DistrictResult>?> GetDistrictsAsync(
        Guid cityId,
        PreferredLanguage language,
        CancellationToken cancellationToken = default);
}
