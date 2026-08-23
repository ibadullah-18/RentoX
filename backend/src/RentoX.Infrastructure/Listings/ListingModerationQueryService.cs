using Microsoft.EntityFrameworkCore;
using RentoX.Application.Listings;
using RentoX.Domain.Catalog.Fields;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Listings;

public sealed class ListingModerationQueryService(
    RentoXDbContext dbContext)
    : IListingModerationQueryService
{
    public async Task<ModerationListingDetailsResult?>
        GetByIdAsync(
            Guid listingId,
            PreferredLanguage language,
            CancellationToken cancellationToken = default)
    {
        Listing? listing =
            await dbContext.Listings
                .AsNoTracking()
                .AsSplitQuery()
                .Include(item => item.Images)
                .Include(item => item.FieldValues)
                    .ThenInclude(value =>
                        value.Selections)
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == listingId &&
                        item.Status ==
                        ListingStatus.PendingReview,
                    cancellationToken);

        if (listing is null)
        {
            return null;
        }

        UserProfile? profile =
            await dbContext.Set<UserProfile>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.Id == listing.OwnerId,
                    cancellationToken);

        string? phoneNumber =
            await dbContext.Users
                .AsNoTracking()
                .Where(user =>
                    user.Id == listing.OwnerId)
                .Select(user => user.PhoneNumber)
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (profile is null ||
            string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        Guid[] fieldIds =
            listing.FieldValues
                .Select(value =>
                    value.CategoryFieldId)
                .Distinct()
                .ToArray();

        List<CategoryField> definitions =
            await dbContext.CategoryFields
                .AsNoTracking()
                .AsSplitQuery()
                .Include(field => field.Translations)
                .Include(field => field.Options)
                    .ThenInclude(option =>
                        option.Translations)
                .Where(field =>
                    fieldIds.Contains(field.Id))
                .ToListAsync(cancellationToken);

        Dictionary<Guid, CategoryField> definitionById =
            definitions.ToDictionary(
                field => field.Id);

        List<ListingImageItemResult> images =
            listing.Images
                .OrderBy(image =>
                    image.DisplayOrder)
                .Select(image =>
                    new ListingImageItemResult(
                        image.Id,
                        image.DisplayOrder,
                        image.IsCover))
                .ToList();

        List<ListingFieldValueDetailsResult> fields =
            listing.FieldValues
                .Select(value =>
                    MapFieldValue(
                        value,
                        definitionById,
                        language))
                .OrderBy(field => field.Key)
                .ToList();

        return new ModerationListingDetailsResult(
            listing.Id,
            listing.OwnerId,
            profile.FullName,
            phoneNumber,
            listing.CategoryId,
            listing.Title,
            listing.Description,
            listing.Price,
            listing.Currency,
            (int)listing.RentalPeriodUnit,
            (int)listing.Status,
            listing.CreatedAtUtc,
            listing.UpdatedAtUtc,
            images,
            fields);
    }

    private static ListingFieldValueDetailsResult
        MapFieldValue(
            ListingFieldValue value,
            Dictionary<Guid, CategoryField>
                definitionById,
            PreferredLanguage language)
    {
        if (!definitionById.TryGetValue(
                value.CategoryFieldId,
                out CategoryField? definition))
        {
            return new ListingFieldValueDetailsResult(
                value.CategoryFieldId,
                "unknown",
                "Unknown",
                0,
                value.TextValue,
                value.NumericValue,
                value.FlagValue,
                value.CalendarValue,
                value.CustomValue,
                []);
        }

        Dictionary<Guid, CategoryFieldOption> optionById =
            definition.Options.ToDictionary(
                option => option.Id);

        List<ListingFieldSelectionValueResult> selections =
            value.Selections
                .Where(selection =>
                    optionById.ContainsKey(
                        selection.CategoryFieldOptionId))
                .Select(selection =>
                {
                    CategoryFieldOption option =
                        optionById[
                            selection.CategoryFieldOptionId];

                    return new ListingFieldSelectionValueResult(
                        option.Id,
                        option.Value,
                        GetOptionLabel(
                            option,
                            language));
                })
                .ToList();

        return new ListingFieldValueDetailsResult(
            definition.Id,
            definition.Key,
            GetFieldLabel(definition, language),
            (int)definition.Type,
            value.TextValue,
            value.NumericValue,
            value.FlagValue,
            value.CalendarValue,
            value.CustomValue,
            selections);
    }

    private static string GetFieldLabel(
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

        return translation?.Label ?? field.Key;
    }

    private static string GetOptionLabel(
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

        return translation?.Label ?? option.Value;
    }
}