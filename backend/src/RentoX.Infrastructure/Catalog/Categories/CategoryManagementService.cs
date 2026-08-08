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
            command.Slug.Trim().ToLowerInvariant();

        bool slugExists =
            await dbContext.Categories.AnyAsync(
                category =>
                    category.Slug == normalizedSlug,
                cancellationToken);

        if (slugExists)
        {
            throw new DomainException(
                "A category with this slug already exists.");
        }

        if (command.ParentId.HasValue)
        {
            bool parentExists =
                await dbContext.Categories.AnyAsync(
                    category =>
                        category.Id == command.ParentId.Value,
                    cancellationToken);

            if (!parentExists)
            {
                throw new DomainException(
                    "Parent category was not found.");
            }
        }

        ValidateTranslations(command.Translations);

        Category category = Category.Create(
            command.ParentId,
            command.Slug,
            command.IconUrl,
            command.DisplayOrder);

        foreach (CategoryTranslationInput input
                 in command.Translations)
        {
            CategoryTranslation translation =
                CategoryTranslation.Create(
                    category.Id,
                    (PreferredLanguage)input.Language,
                    input.Name);

            category.AddTranslation(translation);
        }

        dbContext.Categories.Add(category);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return new CreateCategoryResult(
            category.Id,
            category.ParentId,
            category.Slug,
            category.IconUrl,
            category.DisplayOrder,
            category.IsActive);
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
}