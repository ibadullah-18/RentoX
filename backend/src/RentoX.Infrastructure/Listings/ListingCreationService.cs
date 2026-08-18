using Microsoft.EntityFrameworkCore;
using RentoX.Application.Catalog.Fields;
using RentoX.Application.Listings;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Listings;

public sealed class ListingCreationService(
    RentoXDbContext dbContext,
    ICategoryFieldQueryService fieldQueryService)
    : IListingCreationService
{
    public async Task<CreateListingResult> CreateAsync(
        CreateListingCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Fields);

        if (command.OwnerId == Guid.Empty)
        {
            throw new DomainException(
                "Owner id is required.");
        }

        if (!Enum.IsDefined(
                typeof(RentalPeriodUnit),
                command.RentalPeriodUnit))
        {
            throw new DomainException(
                "Rental period unit is invalid.");
        }

        bool ownerExists =
            await dbContext.Users.AnyAsync(
                user => user.Id == command.OwnerId,
                cancellationToken);

        if (!ownerExists)
        {
            throw new DomainException(
                "Owner account was not found.");
        }

        IReadOnlyList<CategoryFieldDefinitionResult>
            availableFields =
                await fieldQueryService.GetForCategoryAsync(
                    command.CategoryId,
                    PreferredLanguage.Azerbaijani,
                    cancellationToken);

        ValidateDuplicateFields(command.Fields);

        Dictionary<Guid, CategoryFieldDefinitionResult>
            definitions =
                availableFields.ToDictionary(field => field.Id);

        ValidateRequiredFields(
            availableFields,
            command.Fields);

        Listing listing = Listing.Create(
            command.OwnerId,
            command.CategoryId,
            command.Title,
            command.Description,
            command.Price,
            command.Currency,
            (RentalPeriodUnit)command.RentalPeriodUnit);

        foreach (CreateListingFieldInput input
                 in command.Fields)
        {
            if (!definitions.TryGetValue(
                    input.FieldId,
                    out CategoryFieldDefinitionResult?
                        definition))
            {
                throw new DomainException(
                    "A field is not valid for this category.");
            }

            ListingFieldValue fieldValue =
                CreateFieldValue(
                    listing.Id,
                    definition,
                    input);

            listing.AddFieldValue(fieldValue);
        }

        dbContext.Listings.Add(listing);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return new CreateListingResult(
            listing.Id,
            listing.OwnerId,
            listing.CategoryId,
            listing.Title,
            listing.Price,
            listing.Currency,
            (int)listing.RentalPeriodUnit,
            (int)listing.Status,
            listing.CreatedAtUtc);
    }

    private static ListingFieldValue CreateFieldValue(
        Guid listingId,
        CategoryFieldDefinitionResult definition,
        CreateListingFieldInput input)
    {
        ListingFieldValue value =
            ListingFieldValue.Create(
                listingId,
                definition.Id);

        switch (definition.Type)
        {
            case 1:
                value.SetText(
                    RequireText(input.TextValue));
                break;

            case 2:
                decimal wholeNumber =
                    RequireNumber(input.NumericValue);

                if (decimal.Truncate(wholeNumber) !=
                    wholeNumber)
                {
                    throw new DomainException(
                        "Whole number field cannot contain a fractional value.");
                }

                value.SetNumber(wholeNumber);
                break;

            case 3:
                value.SetNumber(
                    RequireNumber(input.NumericValue));
                break;

            case 4:
                value.SetFlag(
                    input.FlagValue
                    ?? throw new DomainException(
                        "Boolean field value is required."));
                break;

            case 5:
                AddSingleSelection(
                    value,
                    definition,
                    input);
                break;

            case 6:
                AddMultipleSelections(
                    value,
                    definition,
                    input);
                break;

            case 7:
                value.SetCalendarDate(
                    input.CalendarValue
                    ?? throw new DomainException(
                        "Date field value is required."));
                break;

            default:
                throw new DomainException(
                    "Category field type is invalid.");
        }

        return value;
    }

    private static void AddSingleSelection(
        ListingFieldValue value,
        CategoryFieldDefinitionResult definition,
        CreateListingFieldInput input)
    {
        Guid[] optionIds =
            input.OptionIds.Distinct().ToArray();

        bool hasCustomValue =
            !string.IsNullOrWhiteSpace(
                input.CustomValue);

        if (optionIds.Length == 0 && !hasCustomValue)
        {
            throw new DomainException(
                "A selection or custom value is required.");
        }

        if (optionIds.Length > 1 ||
            (optionIds.Length == 1 && hasCustomValue))
        {
            throw new DomainException(
                "Single-select field accepts only one value.");
        }

        AddSelections(
            value,
            definition,
            optionIds);

        AddCustomValue(
            value,
            definition,
            input.CustomValue);
    }

    private static void AddMultipleSelections(
        ListingFieldValue value,
        CategoryFieldDefinitionResult definition,
        CreateListingFieldInput input)
    {
        Guid[] optionIds =
            input.OptionIds.Distinct().ToArray();

        bool hasCustomValue =
            !string.IsNullOrWhiteSpace(
                input.CustomValue);

        if (optionIds.Length == 0 && !hasCustomValue)
        {
            throw new DomainException(
                "At least one selection is required.");
        }

        AddSelections(
            value,
            definition,
            optionIds);

        AddCustomValue(
            value,
            definition,
            input.CustomValue);
    }

    private static void AddSelections(
        ListingFieldValue value,
        CategoryFieldDefinitionResult definition,
        IReadOnlyCollection<Guid> optionIds)
    {
        HashSet<Guid> allowedOptionIds =
            definition.Options
                .Select(option => option.Id)
                .ToHashSet();

        foreach (Guid optionId in optionIds)
        {
            if (!allowedOptionIds.Contains(optionId))
            {
                throw new DomainException(
                    "Selected option is not valid for this field.");
            }

            value.AddSelection(optionId);
        }
    }

    private static void AddCustomValue(
        ListingFieldValue value,
        CategoryFieldDefinitionResult definition,
        string? customValue)
    {
        if (string.IsNullOrWhiteSpace(customValue))
        {
            return;
        }

        if (!definition.AllowCustomValue)
        {
            throw new DomainException(
                "Custom value is not allowed for this field.");
        }

        value.SetCustomValue(customValue);
    }

    private static void ValidateRequiredFields(
        IReadOnlyList<CategoryFieldDefinitionResult>
            definitions,
        IReadOnlyList<CreateListingFieldInput> inputs)
    {
        HashSet<Guid> suppliedFieldIds =
            inputs.Select(input => input.FieldId)
                .ToHashSet();

        bool requiredFieldMissing =
            definitions.Any(definition =>
                definition.IsRequired &&
                !suppliedFieldIds.Contains(
                    definition.Id));

        if (requiredFieldMissing)
        {
            throw new DomainException(
                "One or more required category fields are missing.");
        }
    }

    private static void ValidateDuplicateFields(
        IReadOnlyList<CreateListingFieldInput> inputs)
    {
        bool duplicateExists =
            inputs.GroupBy(input => input.FieldId)
                .Any(group => group.Count() > 1);

        if (duplicateExists)
        {
            throw new DomainException(
                "A field cannot be submitted more than once.");
        }
    }

    private static string RequireText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                "Text field value is required.");
        }

        return value;
    }

    private static decimal RequireNumber(
        decimal? value)
    {
        return value
            ?? throw new DomainException(
                "Numeric field value is required.");
    }
}