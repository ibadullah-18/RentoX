using Microsoft.EntityFrameworkCore;
using RentoX.Application.Catalog.Fields;
using RentoX.Domain.Catalog.Fields;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Catalog.Fields;

public sealed class CategoryFieldManagementService(
    RentoXDbContext dbContext)
    : ICategoryFieldManagementService
{
    public async Task<CategoryFieldManagementResult> CreateAsync(
        CreateCategoryFieldCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool categoryExists =
            await dbContext.Categories.AnyAsync(
                category =>
                    category.Id == command.CategoryId,
                cancellationToken);

        if (!categoryExists)
        {
            throw new DomainException(
                "Category was not found.");
        }

        if (!Enum.IsDefined(
                typeof(CategoryFieldType),
                command.Type))
        {
            throw new DomainException(
                "Category field type is invalid.");
        }

        string normalizedKey =
            NormalizeKey(command.Key);

        bool keyExists =
            await dbContext.CategoryFields.AnyAsync(
                field =>
                    field.CategoryId == command.CategoryId &&
                    field.Key == normalizedKey,
                cancellationToken);

        if (keyExists)
        {
            throw new DomainException(
                "A field with this key already exists.");
        }

        ValidateTranslations(command.Translations);

        CategoryFieldType type =
            (CategoryFieldType)command.Type;

        ValidateOptions(
            type,
            command.AllowCustomValue,
            command.Options);

        CategoryField field = CategoryField.Create(
            command.CategoryId,
            normalizedKey,
            type,
            command.IsRequired,
            command.IsFilterable,
            command.IsSearchable,
            command.AllowCustomValue,
            command.AppliesToDescendants,
            command.DisplayOrder);

        foreach (FieldTranslationInput translation
                 in command.Translations)
        {
            field.AddTranslation(
                CategoryFieldTranslation.Create(
                    field.Id,
                    (PreferredLanguage)translation.Language,
                    translation.Label));
        }

        foreach (FieldOptionInput optionInput
                 in command.Options)
        {
            CategoryFieldOption option =
                CategoryFieldOption.Create(
                    field.Id,
                    optionInput.Value,
                    optionInput.DisplayOrder);

            foreach (FieldTranslationInput translation
                     in optionInput.Translations)
            {
                option.AddTranslation(
                    CategoryFieldOptionTranslation.Create(
                        option.Id,
                        (PreferredLanguage)translation.Language,
                        translation.Label));
            }

            field.AddOption(option);
        }

        dbContext.CategoryFields.Add(field);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        CategoryFieldOptionResult[] options =
            field.Options
                .OrderBy(option => option.DisplayOrder)
                .Select(option =>
                    new CategoryFieldOptionResult(
                        option.Id,
                        option.Value,
                        option.DisplayOrder,
                        option.IsActive))
                .ToArray();

        return new CategoryFieldManagementResult(
            field.Id,
            field.CategoryId,
            field.Key,
            (int)field.Type,
            field.IsRequired,
            field.IsFilterable,
            field.IsSearchable,
            field.AllowCustomValue,
            field.AppliesToDescendants,
            field.DisplayOrder,
            field.IsActive,
            options);
    }

    private static void ValidateOptions(
        CategoryFieldType type,
        bool allowCustomValue,
        IReadOnlyList<FieldOptionInput> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        bool isSelectionField =
            type is CategoryFieldType.SingleSelect
                or CategoryFieldType.MultiSelect;

        if (isSelectionField && options.Count == 0)
        {
            throw new DomainException(
                "Selection fields require at least one option.");
        }

        if (!isSelectionField && options.Count > 0)
        {
            throw new DomainException(
                "Only selection fields can contain options.");
        }

        if (!isSelectionField && allowCustomValue)
        {
            throw new DomainException(
                "Custom values are only valid for selection fields.");
        }

        bool duplicateValue =
            options
                .GroupBy(option =>
                    option.Value.Trim().ToLowerInvariant())
                .Any(group => group.Count() > 1);

        if (duplicateValue)
        {
            throw new DomainException(
                "Field option values must be unique.");
        }

        foreach (FieldOptionInput option in options)
        {
            if (string.IsNullOrWhiteSpace(option.Value))
            {
                throw new DomainException(
                    "Field option value is required.");
            }

            if (option.DisplayOrder < 0)
            {
                throw new DomainException(
                    "Option display order cannot be negative.");
            }

            ValidateTranslations(option.Translations);
        }
    }

    private static void ValidateTranslations(
        IReadOnlyList<FieldTranslationInput> translations)
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
                    translation.Label)))
        {
            throw new DomainException(
                "Translation label is required.");
        }
    }

    private static string NormalizeKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainException(
                "Category field key is required.");
        }

        return key.Trim().ToLowerInvariant();
    }
}