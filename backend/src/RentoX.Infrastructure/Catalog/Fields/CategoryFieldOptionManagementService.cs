using Microsoft.EntityFrameworkCore;
using RentoX.Application.Catalog.Fields;
using RentoX.Domain.Catalog.Fields;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Catalog.Fields;

public sealed class CategoryFieldOptionManagementService(
    RentoXDbContext dbContext)
    : ICategoryFieldOptionManagementService
{
    public async Task<CategoryFieldOptionResult> CreateAsync(
        Guid categoryId,
        Guid fieldId,
        SaveCategoryFieldOptionCommand command,
        CancellationToken cancellationToken = default)
    {
        CategoryField field =
            await GetFieldAsync(
                categoryId,
                fieldId,
                cancellationToken);

        EnsureSelectionField(field);
        ValidateCommand(command);

        string normalizedValue =
            NormalizeValue(command.Value);

        bool valueExists =
            await dbContext.CategoryFieldOptions.AnyAsync(
                option =>
                    option.CategoryFieldId == fieldId &&
                    option.Value == normalizedValue,
                cancellationToken);

        if (valueExists)
        {
            throw new DomainException(
                "An option with this value already exists.");
        }

        CategoryFieldOption option =
            CategoryFieldOption.Create(
                fieldId,
                normalizedValue,
                command.DisplayOrder);

        AddTranslations(option, command.Translations);

        field.AddOption(option);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return CreateResult(option);
    }

    public async Task<CategoryFieldOptionResult> UpdateAsync(
        Guid categoryId,
        Guid fieldId,
        Guid optionId,
        SaveCategoryFieldOptionCommand command,
        CancellationToken cancellationToken = default)
    {
        CategoryField field =
            await GetFieldAsync(
                categoryId,
                fieldId,
                cancellationToken);

        EnsureSelectionField(field);
        ValidateCommand(command);

        CategoryFieldOption option =
            await dbContext.CategoryFieldOptions
                .Include(item => item.Translations)
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == optionId &&
                        item.CategoryFieldId == fieldId,
                    cancellationToken)
            ?? throw new DomainException(
                "Category field option was not found.");

        string normalizedValue =
            NormalizeValue(command.Value);

        bool valueExists =
            await dbContext.CategoryFieldOptions.AnyAsync(
                item =>
                    item.CategoryFieldId == fieldId &&
                    item.Value == normalizedValue &&
                    item.Id != optionId,
                cancellationToken);

        if (valueExists)
        {
            throw new DomainException(
                "An option with this value already exists.");
        }

        option.Update(
            normalizedValue,
            command.DisplayOrder);

        foreach (FieldTranslationInput input
                 in command.Translations)
        {
            PreferredLanguage language =
                (PreferredLanguage)input.Language;

            CategoryFieldOptionTranslation? translation =
                option.Translations.SingleOrDefault(item =>
                    item.Language == language);

            if (translation is null)
            {
                option.AddTranslation(
                    CategoryFieldOptionTranslation.Create(
                        option.Id,
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

        return CreateResult(option);
    }

    public async Task SetActiveStatusAsync(
        Guid categoryId,
        Guid fieldId,
        Guid optionId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        await GetFieldAsync(
            categoryId,
            fieldId,
            cancellationToken);

        CategoryFieldOption option =
            await dbContext.CategoryFieldOptions
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == optionId &&
                        item.CategoryFieldId == fieldId,
                    cancellationToken)
            ?? throw new DomainException(
                "Category field option was not found.");

        if (isActive)
        {
            option.Activate();
        }
        else
        {
            option.Deactivate();
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private async Task<CategoryField> GetFieldAsync(
        Guid categoryId,
        Guid fieldId,
        CancellationToken cancellationToken)
    {
        return await dbContext.CategoryFields
            .SingleOrDefaultAsync(
                field =>
                    field.Id == fieldId &&
                    field.CategoryId == categoryId,
                cancellationToken)
            ?? throw new DomainException(
                "Category field was not found.");
    }

    private static void EnsureSelectionField(
        CategoryField field)
    {
        if (field.Type is not
            (CategoryFieldType.SingleSelect or
             CategoryFieldType.MultiSelect))
        {
            throw new DomainException(
                "Only selection fields can contain options.");
        }
    }

    private static void ValidateCommand(
        SaveCategoryFieldOptionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(
            command.Translations);

        if (command.DisplayOrder < 0)
        {
            throw new DomainException(
                "Display order cannot be negative.");
        }

        if (command.Translations.Count != 3)
        {
            throw new DomainException(
                "Azerbaijani, Russian and English translations are required.");
        }

        bool invalidLanguage =
            command.Translations.Any(translation =>
                !Enum.IsDefined(
                    typeof(PreferredLanguage),
                    translation.Language));

        if (invalidLanguage)
        {
            throw new DomainException(
                "Translation language is invalid.");
        }

        bool duplicateLanguage =
            command.Translations
                .GroupBy(translation =>
                    translation.Language)
                .Any(group => group.Count() > 1);

        if (duplicateLanguage)
        {
            throw new DomainException(
                "Translation languages must be unique.");
        }

        if (command.Translations.Any(translation =>
                string.IsNullOrWhiteSpace(
                    translation.Label)))
        {
            throw new DomainException(
                "Translation label is required.");
        }
    }

    private static void AddTranslations(
        CategoryFieldOption option,
        IReadOnlyList<FieldTranslationInput> translations)
    {
        foreach (FieldTranslationInput input
                 in translations)
        {
            option.AddTranslation(
                CategoryFieldOptionTranslation.Create(
                    option.Id,
                    (PreferredLanguage)input.Language,
                    input.Label));
        }
    }

    private static string NormalizeValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                "Option value is required.");
        }

        return value.Trim().ToLowerInvariant();
    }

    private static CategoryFieldOptionResult CreateResult(
        CategoryFieldOption option)
    {
        return new CategoryFieldOptionResult(
            option.Id,
            option.Value,
            option.DisplayOrder,
            option.IsActive);
    }
}