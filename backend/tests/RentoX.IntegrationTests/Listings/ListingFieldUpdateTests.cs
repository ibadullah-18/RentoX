using Microsoft.EntityFrameworkCore;
using RentoX.Application.Listings;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Catalog.Fields;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Catalog.Fields;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Listings;
using RentoX.Infrastructure.Persistence;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Listings;

/// <summary>
/// Replacing the field values of a draft that already has pick-from-a-list
/// values (editing a listing in the app or on the website).
/// </summary>
public sealed class ListingFieldUpdateTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    private sealed record Seed(
        Guid OwnerId,
        Guid ListingId,
        Guid BrandField,
        Guid YearField,
        Guid Toyota,
        Guid Bmw);

    [Fact]
    public async Task ChangingAPickedOptionOfAnExistingValueWorks()
    {
        Seed seed = await SeedAsync();

        await UpdateAsync(seed, seed.Bmw, 2021);

        await using RentoXDbContext db = database.CreateContext();
        Listing saved = await db.Listings
            .Include(l => l.FieldValues).ThenInclude(v => v.Selections)
            .SingleAsync(l => l.Id == seed.ListingId);

        ListingFieldValue brand = Assert.Single(
            saved.FieldValues, v => v.CategoryFieldId == seed.BrandField);
        Assert.Equal(seed.Bmw, Assert.Single(brand.Selections).CategoryFieldOptionId);
        Assert.Equal(2, saved.FieldValues.Count);
    }

    [Fact]
    public async Task SavingTheSameOptionAgainWorks()
    {
        Seed seed = await SeedAsync();

        await UpdateAsync(seed, seed.Toyota, 2020);
        await UpdateAsync(seed, seed.Toyota, 2021);

        await using RentoXDbContext db = database.CreateContext();
        Assert.Equal(
            1,
            await db.Set<ListingFieldSelection>()
                .CountAsync(s => s.CategoryFieldOptionId == seed.Toyota &&
                    db.Set<ListingFieldValue>().Any(v =>
                        v.Id == s.ListingFieldValueId &&
                        v.ListingId == seed.ListingId)));
    }

    private async Task UpdateAsync(Seed seed, Guid brandOption, int year)
    {
        await using RentoXDbContext db = database.CreateContext();
        ListingFieldUpdateService service =
            new(db, new CategoryFieldQueryService(db));

        await service.UpdateAsync(
            new UpdateListingFieldsCommand(
                seed.OwnerId,
                seed.ListingId,
                [
                    new CreateListingFieldInput(
                        seed.BrandField, null, null, null, null, null,
                        [brandOption]),
                    new CreateListingFieldInput(
                        seed.YearField, null, year, null, null, null, [])
                ]));
    }

    private async Task<Seed> SeedAsync()
    {
        await using RentoXDbContext db = database.CreateContext();

        Guid ownerId = Guid.NewGuid();
        string userName = $"field-update-{ownerId:N}";
        db.Users.Add(new AppUser
        {
            Id = ownerId,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            RegisteredAtUtc = Now,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        });

        Category category = Category.Create(
            null, $"upd-{Guid.NewGuid():N}", null, 0);
        category.AddTranslation(
            CategoryTranslation.Create(
                category.Id, PreferredLanguage.Azerbaijani, "Avtomobil"));
        db.Categories.Add(category);

        CategoryField brand = CategoryField.Create(
            category.Id, "brand", CategoryFieldType.SingleSelect,
            isRequired: false, isFilterable: true, isSearchable: false,
            allowCustomValue: false, appliesToDescendants: true,
            displayOrder: 0);
        CategoryField year = CategoryField.Create(
            category.Id, "year", CategoryFieldType.WholeNumber,
            isRequired: false, isFilterable: true, isSearchable: false,
            allowCustomValue: false, appliesToDescendants: true,
            displayOrder: 1);

        CategoryFieldOption toyota = CategoryFieldOption.Create(brand.Id, "Toyota", 0);
        CategoryFieldOption bmw = CategoryFieldOption.Create(brand.Id, "BMW", 1);
        brand.AddOption(toyota);
        brand.AddOption(bmw);
        db.CategoryFields.AddRange(brand, year);

        Listing listing = Listing.Create(
            ownerId, category.Id, "Redaktə testi",
            "Təsvir mətni burada yazılıb.", 60m, "AZN", (RentalPeriodUnit)2);
        listing.MarkAsCreated(Now, ownerId);

        ListingFieldValue brandValue = ListingFieldValue.Create(listing.Id, brand.Id);
        brandValue.AddSelection(toyota.Id);
        listing.AddFieldValue(brandValue);

        ListingFieldValue yearValue = ListingFieldValue.Create(listing.Id, year.Id);
        yearValue.SetNumber(2018);
        listing.AddFieldValue(yearValue);

        db.Listings.Add(listing);
        await db.SaveChangesAsync();

        return new Seed(ownerId, listing.Id, brand.Id, year.Id, toyota.Id, bmw.Id);
    }
}
