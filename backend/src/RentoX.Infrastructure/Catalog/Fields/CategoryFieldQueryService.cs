using Microsoft.EntityFrameworkCore;
using RentoX.Application.Catalog.Fields;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Catalog.Fields;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Catalog.Fields;

public sealed class CategoryFieldQueryService(
    RentoXDbContext dbContext)
    : ICategoryFieldQueryService
{
    public async Task<
        IReadOnlyList<CategoryFieldDefinitionResult>>
        GetForCategoryAsync(
            Guid categoryId,
            PreferredLanguage language,
            CancellationToken cancellationToken = default)
    {
        List<Category> categories =
            await dbContext.Categories
                .AsNoTracking()
                .ToListAsync(cancellationToken);

        Dictionary<Guid, Category> categoryById =
            categories.ToDictionary(category => category.Id);

        if (!categoryById.TryGetValue(
                categoryId,
                out Category? selectedCategory) ||
            !selectedCategory.IsActive)
        {
            throw new DomainException(
                "Category was not found.");
        }

        Dictionary<Guid, int> hierarchyDistance =
            BuildHierarchyDistances(
                selectedCategory,
                categoryById);

        Guid[] hierarchyIds =
            hierarchyDistance.Keys.ToArray();

        List<CategoryField> fields =
            await dbContext.CategoryFields
                .AsNoTracking()
                .Include(field => field.Translations)
                .Include(field => field.Options)
                    .ThenInclude(option =>
                        option.Translations)
                .Where(field =>
                    field.IsActive &&
                    hierarchyIds.Contains(field.CategoryId) &&
                    (field.CategoryId == categoryId ||
                     field.AppliesToDescendants))
                .ToListAsync(cancellationToken);

        List<CategoryField> effectiveFields =
            fields
                .GroupBy(field => field.Key)
                .Select(group =>
                    group
                        .OrderBy(field =>
                            hierarchyDistance[
                                field.CategoryId])
                        .First())
                .OrderBy(field => field.DisplayOrder)
                .ThenBy(field => field.Key)
                .ToList();

        return effectiveFields
            .Select(field => MapField(field, language))
            .ToList();
    }

    private static Dictionary<Guid, int>
       BuildHierarchyDistances(
           Category selectedCategory,
           Dictionary<Guid, Category> categories)
    {
        Dictionary<Guid, int> distances = [];
        HashSet<Guid> visited = [];

        Category? currentCategory = selectedCategory;
        int distance = 0;

        while (currentCategory is not null &&
               visited.Add(currentCategory.Id))
        {
            distances[currentCategory.Id] = distance;
            distance++;

            if (!currentCategory.ParentId.HasValue ||
                !categories.TryGetValue(
                    currentCategory.ParentId.Value,
                    out currentCategory))
            {
                break;
            }
        }

        return distances;
    }

    private static CategoryFieldDefinitionResult MapField(
        CategoryField field,
        PreferredLanguage language)
    {
        CategoryFieldTranslation? translation =
            field.Translations.FirstOrDefault(item =>
                item.Language == language)
            ?? field.Translations.FirstOrDefault(item =>
                item.Language ==
                PreferredLanguage.Azerbaijani)
            ?? field.Translations.FirstOrDefault();

        List<CategoryFieldOptionDefinitionResult> options =
            field.Options
                .Where(option => option.IsActive)
                .OrderBy(option => option.DisplayOrder)
                .Select(option =>
                    MapOption(option, language))
                .ToList();

        return new CategoryFieldDefinitionResult(
            field.Id,
            field.CategoryId,
            field.Key,
            translation?.Label ?? field.Key,
            (int)field.Type,
            field.IsRequired,
            field.IsFilterable,
            field.IsSearchable,
            field.AllowCustomValue,
            field.DisplayOrder,
            options);
    }

    private static CategoryFieldOptionDefinitionResult MapOption(
        CategoryFieldOption option,
        PreferredLanguage language)
    {
        CategoryFieldOptionTranslation? translation =
            option.Translations.FirstOrDefault(item =>
                item.Language == language)
            ?? option.Translations.FirstOrDefault(item =>
                item.Language ==
                PreferredLanguage.Azerbaijani)
            ?? option.Translations.FirstOrDefault();

        return new CategoryFieldOptionDefinitionResult(
            option.Id,
            option.Value,
            translation?.Label ?? option.Value,
            option.DisplayOrder);
    }
}