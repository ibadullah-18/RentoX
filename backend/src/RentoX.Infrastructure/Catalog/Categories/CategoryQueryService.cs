using Microsoft.EntityFrameworkCore;
using RentoX.Application.Catalog.Categories;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Catalog.Categories;

public sealed class CategoryQueryService(
    RentoXDbContext dbContext)
    : ICategoryQueryService
{
    public async Task<IReadOnlyList<CategoryTreeResult>>
        GetTreeAsync(
            PreferredLanguage language,
            CancellationToken cancellationToken = default)
    {
        List<Category> categories =
            await dbContext.Categories
                .AsNoTracking()
                .Include(category =>
                    category.Translations)
                .Where(category => category.IsActive)
                .OrderBy(category =>
                    category.DisplayOrder)
                .ThenBy(category => category.Slug)
                .ToListAsync(cancellationToken);

        ILookup<Guid?, Category> lookup =
            categories.ToLookup(category =>
                category.ParentId);

        return BuildChildren(
            null,
            language,
            lookup,
            [],
            Category.AllRentalPeriodsMask);
    }

    private static List<CategoryTreeResult>
    BuildChildren(
            Guid? parentId,
            PreferredLanguage language,
            ILookup<Guid?, Category> lookup,
            HashSet<Guid> parentPath,
            int inheritedMask)
    {
        List<CategoryTreeResult> result = [];

        foreach (Category category in lookup[parentId])
        {
            if (!parentPath.Add(category.Id))
            {
                continue;
            }

            CategoryTranslation? translation =
                category.Translations.FirstOrDefault(item =>
                    item.Language == language)
                ?? category.Translations.FirstOrDefault(item =>
                    item.Language ==
                    PreferredLanguage.Azerbaijani)
                ?? category.Translations.FirstOrDefault();

            // Own choice, else the parent's (or everything at the top).
            int effectiveMask =
                category.RentalPeriodMask != 0
                    ? category.RentalPeriodMask
                    : inheritedMask;

            List<CategoryTreeResult> children =
                BuildChildren(
                    category.Id,
                    language,
                    lookup,
                    parentPath,
                    effectiveMask);

            result.Add(new CategoryTreeResult(
                category.Id,
                category.ParentId,
                category.Slug,
                translation?.Name ?? category.Slug,
                category.IconUrl,
                category.DisplayOrder,
                children)
            {
                RentalPeriods = Category.PeriodsOf(effectiveMask)
            });

            parentPath.Remove(category.Id);
        }

        return result;
    }
}