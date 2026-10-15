using Microsoft.EntityFrameworkCore;
using RentoX.Application.Admin;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Admin;

public sealed class AdminCatalogReadService(RentoXDbContext dbContext) : IAdminCatalogReadService
{
    public async Task<IReadOnlyList<AdminCategoryResult>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Categories
            .AsNoTracking()
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Slug)
            .Select(c => new
            {
                c.Id,
                c.ParentId,
                c.Slug,
                c.IconUrl,
                c.DisplayOrder,
                c.IsActive,
                c.RentalPeriodMask,
                Translations = c.Translations
                    .Select(t => new AdminTranslation((int)t.Language, t.Name))
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(c => new AdminCategoryResult(
                c.Id,
                c.ParentId,
                c.Slug,
                c.IconUrl,
                c.DisplayOrder,
                c.IsActive,
                // Bit (unit - 1) of the mask is one rental period.
                Enumerable.Range(1, 6).Where(unit => (c.RentalPeriodMask & (1 << (unit - 1))) != 0).ToArray(),
                c.Translations.OrderBy(t => t.Language).ToArray()))
            .ToArray();
    }

    public async Task<IReadOnlyList<AdminFieldResult>> GetFieldsAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var fields = await dbContext.CategoryFields
            .AsNoTracking()
            .Where(f => f.CategoryId == categoryId)
            .OrderBy(f => f.DisplayOrder)
            .ThenBy(f => f.Key)
            .Select(f => new
            {
                f.Id,
                f.CategoryId,
                f.Key,
                Type = (int)f.Type,
                f.IsRequired,
                f.IsFilterable,
                f.IsSearchable,
                f.AllowCustomValue,
                f.AppliesToDescendants,
                f.DisplayOrder,
                f.IsActive,
                Translations = f.Translations
                    .Select(t => new AdminTranslation((int)t.Language, t.Label))
                    .ToList(),
                Options = f.Options
                    .OrderBy(o => o.DisplayOrder)
                    .Select(o => new
                    {
                        o.Id,
                        o.Value,
                        o.DisplayOrder,
                        o.IsActive,
                        Translations = o.Translations
                            .Select(t => new AdminTranslation((int)t.Language, t.Label))
                            .ToList()
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return fields
            .Select(f => new AdminFieldResult(
                f.Id,
                f.CategoryId,
                f.Key,
                f.Type,
                f.IsRequired,
                f.IsFilterable,
                f.IsSearchable,
                f.AllowCustomValue,
                f.AppliesToDescendants,
                f.DisplayOrder,
                f.IsActive,
                f.Translations.OrderBy(t => t.Language).ToArray(),
                f.Options
                    .Select(o => new AdminFieldOptionResult(
                        o.Id,
                        o.Value,
                        o.DisplayOrder,
                        o.IsActive,
                        o.Translations.OrderBy(t => t.Language).ToArray()))
                    .ToArray()))
            .ToArray();
    }
}
