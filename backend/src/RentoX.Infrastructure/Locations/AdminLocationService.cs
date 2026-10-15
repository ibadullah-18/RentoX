using Microsoft.EntityFrameworkCore;
using RentoX.Application.Locations;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Locations;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Locations;

public sealed class AdminLocationService(RentoXDbContext dbContext) : IAdminLocationService
{
    public async Task<IReadOnlyList<AdminCityItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        List<City> cities = await dbContext.Cities.AsNoTracking()
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.NameAz)
            .ToListAsync(cancellationToken);
        List<District> districts = await dbContext.Districts.AsNoTracking()
            .OrderBy(d => d.DisplayOrder).ThenBy(d => d.NameAz)
            .ToListAsync(cancellationToken);

        // Listings still on show or waiting, not the deleted ones.
        Dictionary<Guid, int> byCity = await dbContext.Listings.AsNoTracking()
            .Where(l => l.CityId != null && l.Status != ListingStatus.Deleted)
            .GroupBy(l => l.CityId!.Value)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
        Dictionary<Guid, int> byDistrict = await dbContext.Listings.AsNoTracking()
            .Where(l => l.DistrictId != null && l.Status != ListingStatus.Deleted)
            .GroupBy(l => l.DistrictId!.Value)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        return cities.Select(c => new AdminCityItem(
            c.Id, c.NameAz, c.NameRu, c.NameEn, c.DisplayOrder, c.IsActive,
            byCity.GetValueOrDefault(c.Id),
            districts.Where(d => d.CityId == c.Id).Select(d => new AdminDistrictItem(
                d.Id, d.NameAz, d.NameRu, d.NameEn, d.DisplayOrder, d.IsActive,
                byDistrict.GetValueOrDefault(d.Id))).ToArray())).ToArray();
    }

    public async Task<Guid> CreateCityAsync(
        string nameAz, string nameRu, string nameEn, int displayOrder,
        CancellationToken cancellationToken = default)
    {
        City city = City.Create(nameAz, nameRu, nameEn, ValidOrder(displayOrder));
        await EnsureCityNameFreeAsync(city.NameAz, city.Id, cancellationToken);
        dbContext.Cities.Add(city);
        await dbContext.SaveChangesAsync(cancellationToken);
        return city.Id;
    }

    public async Task UpdateCityAsync(
        Guid cityId, string nameAz, string nameRu, string nameEn, int displayOrder, bool isActive,
        CancellationToken cancellationToken = default)
    {
        City city = await dbContext.Cities.SingleOrDefaultAsync(c => c.Id == cityId, cancellationToken)
            ?? throw new DomainException("City was not found.");
        city.Update(nameAz, nameRu, nameEn, ValidOrder(displayOrder));
        await EnsureCityNameFreeAsync(city.NameAz, city.Id, cancellationToken);
        if (isActive)
        {
            city.Activate();
        }
        else
        {
            city.Deactivate();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateDistrictAsync(
        Guid cityId, string nameAz, string nameRu, string nameEn, int displayOrder,
        CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Cities.AnyAsync(c => c.Id == cityId, cancellationToken))
        {
            throw new DomainException("City was not found.");
        }

        District district = District.Create(cityId, nameAz, nameRu, nameEn, ValidOrder(displayOrder));
        await EnsureDistrictNameFreeAsync(cityId, district.NameAz, district.Id, cancellationToken);
        dbContext.Districts.Add(district);
        await dbContext.SaveChangesAsync(cancellationToken);
        return district.Id;
    }

    public async Task UpdateDistrictAsync(
        Guid districtId, string nameAz, string nameRu, string nameEn, int displayOrder, bool isActive,
        CancellationToken cancellationToken = default)
    {
        District district = await dbContext.Districts.SingleOrDefaultAsync(d => d.Id == districtId, cancellationToken)
            ?? throw new DomainException("District was not found.");
        district.Update(nameAz, nameRu, nameEn, ValidOrder(displayOrder));
        await EnsureDistrictNameFreeAsync(district.CityId, district.NameAz, district.Id, cancellationToken);
        if (isActive)
        {
            district.Activate();
        }
        else
        {
            district.Deactivate();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCityNameFreeAsync(string nameAz, Guid exceptId, CancellationToken cancellationToken)
    {
        if (await dbContext.Cities.AnyAsync(c => c.NameAz == nameAz && c.Id != exceptId, cancellationToken))
        {
            throw new DomainException("A city with this name already exists.");
        }
    }

    private async Task EnsureDistrictNameFreeAsync(
        Guid cityId, string nameAz, Guid exceptId, CancellationToken cancellationToken)
    {
        if (await dbContext.Districts.AnyAsync(
                d => d.CityId == cityId && d.NameAz == nameAz && d.Id != exceptId, cancellationToken))
        {
            throw new DomainException("A district with this name already exists in this city.");
        }
    }

    private static int ValidOrder(int order)
    {
        if (order is < 0 or > 10000)
        {
            throw new DomainException("Display order must be between 0 and 10000.");
        }

        return order;
    }
}
