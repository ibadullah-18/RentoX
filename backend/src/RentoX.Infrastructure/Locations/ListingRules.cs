using Microsoft.EntityFrameworkCore;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Locations;

/// <summary>
/// Rules a listing has to follow that depend on reference data: where it
/// is, and which rental periods its category allows.
/// </summary>
internal static class ListingRules
{
    /// <summary>
    /// Checks the city (always required when <paramref name="required"/>) and
    /// the district (required when the city has districts).
    /// </summary>
    public static async Task ValidateLocationAsync(
        RentoXDbContext dbContext,
        Guid? cityId,
        Guid? districtId,
        bool required,
        CancellationToken cancellationToken)
    {
        if (!cityId.HasValue)
        {
            if (districtId.HasValue)
            {
                throw new DomainException(
                    "A district needs its city.");
            }

            if (required)
            {
                throw new DomainException("City is required.");
            }

            return;
        }

        bool cityOk =
            await dbContext.Cities
                .AsNoTracking()
                .AnyAsync(
                    city => city.Id == cityId.Value && city.IsActive,
                    cancellationToken);

        if (!cityOk)
        {
            throw new DomainException("City was not found.");
        }

        if (districtId.HasValue)
        {
            bool districtOk =
                await dbContext.Districts
                    .AsNoTracking()
                    .AnyAsync(
                        district =>
                            district.Id == districtId.Value &&
                            district.CityId == cityId.Value &&
                            district.IsActive,
                        cancellationToken);

            if (!districtOk)
            {
                throw new DomainException(
                    "District was not found in this city.");
            }

            return;
        }

        if (required)
        {
            bool needsDistrict =
                await dbContext.Districts
                    .AsNoTracking()
                    .AnyAsync(
                        district =>
                            district.CityId == cityId.Value &&
                            district.IsActive,
                        cancellationToken);

            if (needsDistrict)
            {
                throw new DomainException(
                    "District is required for this city.");
            }
        }
    }

    /// <summary>
    /// The rental periods the category allows: its own choice, else the
    /// nearest parent's, else all of them.
    /// </summary>
    public static async Task<int> AllowedPeriodsMaskAsync(
        RentoXDbContext dbContext,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        Guid? current = categoryId;

        for (int depth = 0; current.HasValue && depth < 10; depth++)
        {
            var node =
                await dbContext.Categories
                    .AsNoTracking()
                    .Where(category => category.Id == current.Value)
                    .Select(category => new
                    {
                        category.RentalPeriodMask,
                        category.ParentId
                    })
                    .SingleOrDefaultAsync(cancellationToken);

            if (node is null)
            {
                break;
            }

            if (node.RentalPeriodMask != 0)
            {
                return node.RentalPeriodMask;
            }

            current = node.ParentId;
        }

        return Category.AllRentalPeriodsMask;
    }

    public static async Task ValidateRentalPeriodAsync(
        RentoXDbContext dbContext,
        Guid categoryId,
        RentalPeriodUnit unit,
        CancellationToken cancellationToken)
    {
        int mask =
            await AllowedPeriodsMaskAsync(
                dbContext,
                categoryId,
                cancellationToken);

        if ((mask & (1 << ((int)unit - 1))) == 0)
        {
            throw new DomainException(
                "This rental period is not allowed in this category.");
        }
    }
}
