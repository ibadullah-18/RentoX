using Microsoft.EntityFrameworkCore;
using RentoX.Application.Locations;
using RentoX.Domain.Locations;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Locations;

public sealed class LocationQueryService(RentoXDbContext dbContext)
    : ILocationQueryService
{
    public async Task<IReadOnlyList<CityResult>> GetCitiesAsync(
        PreferredLanguage language,
        CancellationToken cancellationToken = default)
    {
        List<City> cities =
            await dbContext.Cities
                .AsNoTracking()
                .Where(city => city.IsActive)
                .OrderBy(city => city.DisplayOrder)
                .ThenBy(city => city.NameAz)
                .ToListAsync(cancellationToken);

        HashSet<Guid> withDistricts =
            (await dbContext.Districts
                .AsNoTracking()
                .Where(district => district.IsActive)
                .Select(district => district.CityId)
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToHashSet();

        return
        [
            .. cities.Select(city => new CityResult(
                city.Id,
                city.NameFor(language),
                withDistricts.Contains(city.Id)))
        ];
    }

    public async Task<IReadOnlyList<DistrictResult>?> GetDistrictsAsync(
        Guid cityId,
        PreferredLanguage language,
        CancellationToken cancellationToken = default)
    {
        bool cityExists =
            await dbContext.Cities
                .AsNoTracking()
                .AnyAsync(
                    city => city.Id == cityId && city.IsActive,
                    cancellationToken);

        if (!cityExists)
        {
            return null;
        }

        List<District> districts =
            await dbContext.Districts
                .AsNoTracking()
                .Where(district =>
                    district.CityId == cityId && district.IsActive)
                .OrderBy(district => district.DisplayOrder)
                .ThenBy(district => district.NameAz)
                .ToListAsync(cancellationToken);

        return
        [
            .. districts.Select(district => new DistrictResult(
                district.Id,
                district.NameFor(language)))
        ];
    }
}
