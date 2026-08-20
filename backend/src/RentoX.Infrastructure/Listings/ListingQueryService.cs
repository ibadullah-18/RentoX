using Microsoft.EntityFrameworkCore;
using RentoX.Application.Common;
using RentoX.Application.Listings;
using RentoX.Domain.Catalog.Fields;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Listings;

public sealed class ListingQueryService(
    RentoXDbContext dbContext)
    : IListingQueryService
{
    private const int MaximumPageSize = 50;

    public async Task<
        PagedResult<OwnedListingSummaryResult>>
        GetMineAsync(
            Guid ownerId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        ValidatePagination(page, pageSize);

        IQueryable<Listing> query =
            dbContext.Listings
                .AsNoTracking()
                .Where(listing =>
                    listing.OwnerId == ownerId);

        int totalCount =
            await query.CountAsync(
                cancellationToken);

        List<OwnedListingSummaryResult> items =
            await query
                .OrderByDescending(listing =>
                    listing.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(listing =>
                    new OwnedListingSummaryResult(
                        listing.Id,
                        listing.CategoryId,
                        listing.Title,
                        listing.Price,
                        listing.Currency,
                        (int)listing.RentalPeriodUnit,
                        (int)listing.Status,
                        listing.Images
                            .Where(image => image.IsCover)
                            .Select(image =>
                                (Guid?)image.Id)
                            .FirstOrDefault(),
                        listing.Images.Count,
                        listing.CreatedAtUtc,
                        listing.PublishedAtUtc,
                        listing.ExpiresAtUtc))
                .ToListAsync(cancellationToken);

        int totalPages =
            totalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    totalCount /
                    (double)pageSize);

        return new PagedResult<
            OwnedListingSummaryResult>(
                items,
                page,
                pageSize,
                totalCount,
                totalPages);
    }

    public async Task<OwnedListingDetailsResult?>
        GetMineByIdAsync(
            Guid ownerId,
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
                        item.OwnerId == ownerId,
                    cancellationToken);

        if (listing is null)
        {
            return null;
        }

        Guid[] fieldIds =
            listing.FieldValues
                .Select(value =>
                    value.CategoryFieldId)
                .Distinct()
                .ToArray();

        List<CategoryField> fieldDefinitions =
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

        Dictionary<Guid, CategoryField> fieldById =
            fieldDefinitions.ToDictionary(
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
                        fieldById,
                        language))
                .OrderBy(field => field.Key)
                .ToList();

        return new OwnedListingDetailsResult(
            listing.Id,
            listing.OwnerId,
            listing.CategoryId,
            listing.Title,
            listing.Description,
            listing.Price,
            listing.Currency,
            (int)listing.RentalPeriodUnit,
            (int)listing.Status,
            listing.RejectionReason,
            listing.CreatedAtUtc,
            listing.UpdatedAtUtc,
            listing.PublishedAtUtc,
            listing.ExpiresAtUtc,
            images,
            fields);
    }

    private static ListingFieldValueDetailsResult MapFieldValue(
    ListingFieldValue value,
    Dictionary<Guid, CategoryField> fieldById,
    PreferredLanguage language)
    {
        if (!fieldById.TryGetValue(
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

        string label =
            GetFieldLabel(definition, language);

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
                        GetOptionLabel(option, language));
                })
                .ToList();

        return new ListingFieldValueDetailsResult(
            definition.Id,
            definition.Key,
            label,
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

    private static void ValidatePagination(
        int page,
        int pageSize)
    {
        if (page <= 0)
        {
            throw new DomainException(
                "Page must be greater than zero.");
        }

        if (pageSize <= 0 ||
            pageSize > MaximumPageSize)
        {
            throw new DomainException(
                "Page size must be between 1 and 50.");
        }
    }
}