using Microsoft.EntityFrameworkCore;
using RentoX.Application.Catalog.Fields;
using RentoX.Domain.Catalog.Fields;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Catalog.Fields;

public sealed class CategoryFieldAdministrationService(
    RentoXDbContext dbContext)
    : ICategoryFieldAdministrationService
{
    public async Task<CategoryFieldManagementResult> UpdateAsync(
        Guid fieldId,
        UpdateCategoryFieldCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        CategoryField field =
            await dbContext.CategoryFields
                .Include(item => item.Translations)
                .Include(item => item.Options)
                .SingleOrDefaultAsync(
                    item => item.Id == fieldId,
                    cancellationToken)
            ?? throw new DomainException(
                "Category field was not found.");

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
                item =>
                    item.CategoryId == field.CategoryId &&
                    item.Key == normalizedKey &&
                    item.Id != fieldId,
                cancellationToken);

        if (keyExists)
        {
            throw new DomainException(
                "A field with this key already exists.");
        }

        ValidateTranslations(command.Translations);

        CategoryFieldType type =
            (CategoryFieldType)command.Type;

        bool isSelectionField =
            type is CategoryFieldType.SingleSelect
                or CategoryFieldType.MultiSelect;

        if (!isSelectionField &&
            field.Options.Count > 0)
        {
            throw new DomainException(
                "A field containing options must remain a selection field.");
        }

        if (!isSelectionField &&
            command.AllowCustomValue)
        {
            throw new DomainException(
                "Custom values are only valid for selection fields.");
        }

        field.Update(
            normalizedKey,
            type,
            command.IsRequired,
            command.IsFilterable,
            command.IsSearchable,
            command.AllowCustomValue,
            command.AppliesToDescendants,
            command.DisplayOrder);

        foreach (FieldTranslationInput input
                 in command.Translations)
        {
            PreferredLanguage language =
                (PreferredLanguage)input.Language;

            CategoryFieldTranslation? translation =
                field.Translations.SingleOrDefault(item =>
                    item.Language == language);

            if (translation is null)
            {
                field.AddTranslation(
                    CategoryFieldTranslation.Create(
                        field.Id,
                        language,
                        input.Label));
            }
            else
            {
                translation.UpdateLabel(input.Label);
            }
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return CreateResult(field);
    }

    public async Task SetActiveStatusAsync(
        Guid fieldId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        CategoryField field =
            await dbContext.CategoryFields
                .SingleOrDefaultAsync(
                    item => item.Id == fieldId,
                    cancellationToken)
            ?? throw new DomainException(
                "Category field was not found.");

        if (isActive)
        {
            field.Activate();
        }
        else
        {
            field.Deactivate();
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);
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

    private static CategoryFieldManagementResult CreateResult(
        CategoryField field)
    {
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
}