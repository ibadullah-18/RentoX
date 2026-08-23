using Microsoft.EntityFrameworkCore;
using RentoX.Application.Catalog.Fields;
using RentoX.Application.Listings;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Listings;

public sealed class ListingSubmissionService(
    RentoXDbContext dbContext,
    ICategoryFieldQueryService fieldQueryService)
    : IListingSubmissionService
{
    public async Task<SubmitListingResult> SubmitAsync(
        SubmitListingCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        Listing listing =
            await dbContext.Listings
                .Include(item => item.Images)
                .Include(item => item.FieldValues)
                    .ThenInclude(value =>
                        value.Selections)
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == command.ListingId &&
                        item.OwnerId == command.OwnerId,
                    cancellationToken)
            ?? throw new DomainException(
                "Listing was not found.");

        UserProfile? profile =
            await dbContext.Set<UserProfile>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.Id == command.OwnerId,
                    cancellationToken);

        if (profile is null ||
            profile.Status != UserStatus.Active)
        {
            throw new DomainException(
                "User profile is not active.");
        }

        bool categoryIsActive =
            await dbContext.Categories
                .AsNoTracking()
                .AnyAsync(
                    category =>
                        category.Id == listing.CategoryId &&
                        category.IsActive,
                    cancellationToken);

        if (!categoryIsActive)
        {
            throw new DomainException(
                "Listing category is not active.");
        }

        IReadOnlyList<CategoryFieldDefinitionResult>
            definitions =
                await fieldQueryService.GetForCategoryAsync(
                    listing.CategoryId,
                    profile.PreferredLanguage,
                    cancellationToken);

        ValidateFields(
            listing,
            definitions);

        listing.SubmitForReview();

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return new SubmitListingResult(
            listing.Id,
            (int)listing.Status,
            listing.UpdatedAtUtc);
    }

    private static void ValidateFields(
        Listing listing,
        IReadOnlyList<CategoryFieldDefinitionResult>
            definitions)
    {
        Dictionary<Guid, CategoryFieldDefinitionResult>
            definitionById =
                definitions.ToDictionary(
                    definition => definition.Id);

        HashSet<Guid> suppliedFieldIds =
            listing.FieldValues
                .Select(value =>
                    value.CategoryFieldId)
                .ToHashSet();

        bool requiredFieldMissing =
            definitions.Any(definition =>
                definition.IsRequired &&
                !suppliedFieldIds.Contains(
                    definition.Id));

        if (requiredFieldMissing)
        {
            throw new DomainException(
                "One or more required fields are missing.");
        }

        foreach (ListingFieldValue fieldValue
                 in listing.FieldValues)
        {
            if (!definitionById.TryGetValue(
                    fieldValue.CategoryFieldId,
                    out CategoryFieldDefinitionResult?
                        definition))
            {
                throw new DomainException(
                    "A listing field is no longer active.");
            }

            ValidateSelections(
                fieldValue,
                definition);
        }
    }

    private static void ValidateSelections(
        ListingFieldValue fieldValue,
        CategoryFieldDefinitionResult definition)
    {
        if (definition.Type is not (5 or 6))
        {
            return;
        }

        HashSet<Guid> activeOptionIds =
            definition.Options
                .Select(option => option.Id)
                .ToHashSet();

        bool invalidOptionExists =
            fieldValue.Selections.Any(selection =>
                !activeOptionIds.Contains(
                    selection.CategoryFieldOptionId));

        if (invalidOptionExists)
        {
            throw new DomainException(
                "A selected field option is no longer active.");
        }

        bool hasSelection =
            fieldValue.Selections.Count > 0;

        bool hasCustomValue =
            !string.IsNullOrWhiteSpace(
                fieldValue.CustomValue);

        if (!hasSelection && !hasCustomValue)
        {
            throw new DomainException(
                "A selection field has no value.");
        }

        if (hasCustomValue &&
            !definition.AllowCustomValue)
        {
            throw new DomainException(
                "Custom value is no longer allowed.");
        }

        if (definition.Type == 5 &&
            fieldValue.Selections.Count > 1)
        {
            throw new DomainException(
                "Single-select field contains multiple options.");
        }
    }
}