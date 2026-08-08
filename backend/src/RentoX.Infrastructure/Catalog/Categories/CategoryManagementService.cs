using Microsoft.EntityFrameworkCore;
using RentoX.Application.Catalog.Categories;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Catalog.Categories;

public sealed class CategoryManagementService(
    RentoXDbContext dbContext)
    : ICategoryManagementService
{
    public async Task<CreateCategoryResult> CreateAsync(
        CreateCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        string normalizedSlug =
            NormalizeSlug(command.Slug);

        await EnsureSlugIsUniqueAsync(
            normalizedSlug,
            null,
            cancellationToken);

        await EnsureParentExistsAsync(
            command.ParentId,
            cancellationToken);

        ValidateTranslations(command.Translations);

        Category category = Category.Create(
            command.ParentId,
            normalizedSlug,
            command.IconUrl,
            command.DisplayOrder);

        AddTranslations(
            category,
            command.Translations);

        dbContext.Categories.Add(category);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return CreateResult(category);
    }

    public async Task<CreateCategoryResult> UpdateAsync(
        Guid categoryId,
        UpdateCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        Category category =
            await dbContext.Categories
                .Include(item => item.Translations)
                .SingleOrDefaultAsync(
                    item => item.Id == categoryId,
                    cancellationToken)
            ?? throw new DomainException(
                "Category was not found.");

        string normalizedSlug =
            NormalizeSlug(command.Slug);

        await EnsureSlugIsUniqueAsync(
            normalizedSlug,
            categoryId,
            cancellationToken);

        await EnsureParentExistsAsync(
            command.ParentId,
            cancellationToken);

        await EnsureParentDoesNotCreateCycleAsync(
            categoryId,
            command.ParentId,
            cancellationToken);

        ValidateTranslations(command.Translations);

        category.Update(
            command.ParentId,
            normalizedSlug,
            command.IconUrl,
            command.DisplayOrder);

        foreach (CategoryTranslationInput input
                 in command.Translations)
        {
            PreferredLanguage language =
                (PreferredLanguage)input.Language;

            CategoryTranslation? translation =
                category.Translations
                    .SingleOrDefault(item =>
                        item.Language == language);

            if (translation is null)
            {
                category.AddTranslation(
                    CategoryTranslation.Create(
                        category.Id,
                        language,
                        input.Name));
            }
            else
            {
                translation.UpdateName(input.Name);
            }
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return CreateResult(category);
    }

    public async Task SetActiveStatusAsync(
        Guid categoryId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        Category category =
            await dbContext.Categories
                .SingleOrDefaultAsync(
                    item => item.Id == categoryId,
                    cancellationToken)
            ?? throw new DomainException(
                "Category was not found.");

        if (isActive)
        {
            category.Activate();
        }
        else
        {
            category.Deactivate();
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private async Task EnsureSlugIsUniqueAsync(
        string slug,
        Guid? excludedCategoryId,
        CancellationToken cancellationToken)
    {
        bool exists =
            await dbContext.Categories.AnyAsync(
                category =>
                    category.Slug == slug &&
                    category.Id != excludedCategoryId,
                cancellationToken);

        if (exists)
        {
            throw new DomainException(
                "A category with this slug already exists.");
        }
    }

    private async Task EnsureParentExistsAsync(
        Guid? parentId,
        CancellationToken cancellationToken)
    {
        if (!parentId.HasValue)
        {
            return;
        }

        bool exists =
            await dbContext.Categories.AnyAsync(
                category => category.Id == parentId.Value,
                cancellationToken);

        if (!exists)
        {
            throw new DomainException(
                "Parent category was not found.");
        }
    }

    private async Task EnsureParentDoesNotCreateCycleAsync(
        Guid categoryId,
        Guid? parentId,
        CancellationToken cancellationToken)
    {
        Guid? currentId = parentId;

        while (currentId.HasValue)
        {
            if (currentId.Value == categoryId)
            {
                throw new DomainException(
                    "Category hierarchy cannot contain a cycle.");
            }

            currentId =
                await dbContext.Categories
                    .Where(category =>
                        category.Id == currentId.Value)
                    .Select(category =>
                        category.ParentId)
                    .SingleOrDefaultAsync(
                        cancellationToken);
        }
    }

    private static void AddTranslations(
        Category category,
        IReadOnlyList<CategoryTranslationInput> translations)
    {
        foreach (CategoryTranslationInput input
                 in translations)
        {
            category.AddTranslation(
                CategoryTranslation.Create(
                    category.Id,
                    (PreferredLanguage)input.Language,
                    input.Name));
        }
    }

    private static void ValidateTranslations(
        IReadOnlyList<CategoryTranslationInput> translations)
    {
        ArgumentNullException.ThrowIfNull(translations);

        if (translations.Count != 3)
        {
            throw new DomainException(
                "Azerbaijani, Russian and English translations are required.");
        }

        bool invalidLanguage =
            translations.Any(translation =>
                !Enum.IsDefined(
                    typeof(PreferredLanguage),
                    translation.Language));

        if (invalidLanguage)
        {
            throw new DomainException(
                "Translation language is invalid.");
        }

        bool duplicateLanguage =
            translations
                .GroupBy(translation =>
                    translation.Language)
                .Any(group => group.Count() > 1);

        if (duplicateLanguage)
        {
            throw new DomainException(
                "Translation languages must be unique.");
        }

        if (translations.Any(translation =>
                string.IsNullOrWhiteSpace(
                    translation.Name)))
        {
            throw new DomainException(
                "Translation name is required.");
        }
    }

    private static string NormalizeSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new DomainException(
                "Category slug is required.");
        }

        return slug.Trim().ToLowerInvariant();
    }

    private static CreateCategoryResult CreateResult(
        Category category)
    {
        return new CreateCategoryResult(
            category.Id,
            category.ParentId,
            category.Slug,
            category.IconUrl,
            category.DisplayOrder,
            category.IsActive);
    }
}