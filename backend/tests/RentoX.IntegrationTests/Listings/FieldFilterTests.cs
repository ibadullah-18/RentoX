using RentoX.Application.Abstractions.Time;
using RentoX.Application.Common;
using RentoX.Application.Listings;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Catalog.Fields;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Listings;
using RentoX.Infrastructure.Persistence;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Listings;

public sealed class FieldFilterTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

    private static readonly SemaphoreSlim SeedGate = new(1, 1);
    private static bool seeded;

    private static Guid categoryId;
    private static Guid brandField, gearboxField, yearField, electricField;
    private static Guid toyota, bmw, automatic, manual;

    [Fact]
    public async Task OptionsOfOneFieldAreAlternativesAndFieldsAreCombined()
    {
        await EnsureSeededAsync();

        // (Toyota or BMW) and automatic
        List<string> titles = await SearchAsync(
            new PublicListingFieldFilter(brandField, [toyota, bmw]),
            new PublicListingFieldFilter(gearboxField, [automatic]));

        Assert.Equal(2, titles.Count);
        Assert.Contains("Toyota auto 2022", titles);
        Assert.Contains("BMW auto 2021", titles);
    }

    [Fact]
    public async Task SelectAndNumberFilterWorkTogether()
    {
        await EnsureSeededAsync();

        List<string> titles = await SearchAsync(
            new PublicListingFieldFilter(brandField, [toyota]),
            new PublicListingFieldFilter(yearField, Min: 2020));

        Assert.Equal(["Toyota auto 2022"], titles);
    }

    [Fact]
    public async Task ThreeDifferentFieldTypesCanBeCombined()
    {
        await EnsureSeededAsync();

        List<string> titles = await SearchAsync(
            new PublicListingFieldFilter(gearboxField, [automatic]),
            new PublicListingFieldFilter(yearField, Min: 2020, Max: 2025),
            new PublicListingFieldFilter(electricField, Flag: true));

        Assert.Equal(["Toyota auto 2022"], titles);
    }

    [Fact]
    public async Task NumberRangeWithOnlyAMaximumWorks()
    {
        await EnsureSeededAsync();

        List<string> titles = await SearchAsync(
            new PublicListingFieldFilter(yearField, Max: 2018));

        Assert.Equal(2, titles.Count);
        Assert.Contains("Toyota manual 2018", titles);
        Assert.Contains("BMW manual 2015", titles);
    }

    [Fact]
    public async Task NothingMatchingGivesAnEmptyPage()
    {
        await EnsureSeededAsync();

        Assert.Empty(await SearchAsync(
            new PublicListingFieldFilter(brandField, [bmw]),
            new PublicListingFieldFilter(electricField, Flag: true)));
    }

    [Fact]
    public async Task BadFiltersAreRejected()
    {
        await EnsureSeededAsync();

        // Same field twice.
        await Assert.ThrowsAsync<DomainException>(() => SearchAsync(
            new PublicListingFieldFilter(brandField, [toyota]),
            new PublicListingFieldFilter(brandField, [bmw])));

        // Unknown field.
        await Assert.ThrowsAsync<DomainException>(() => SearchAsync(
            new PublicListingFieldFilter(Guid.NewGuid(), [toyota])));

        // An option that belongs to another field.
        await Assert.ThrowsAsync<DomainException>(() => SearchAsync(
            new PublicListingFieldFilter(brandField, [automatic])));

        // A select filter without options, a number filter without bounds,
        // a yes/no filter without a value.
        await Assert.ThrowsAsync<DomainException>(() => SearchAsync(
            new PublicListingFieldFilter(brandField)));
        await Assert.ThrowsAsync<DomainException>(() => SearchAsync(
            new PublicListingFieldFilter(yearField)));
        await Assert.ThrowsAsync<DomainException>(() => SearchAsync(
            new PublicListingFieldFilter(electricField)));

        // Minimum above maximum.
        await Assert.ThrowsAsync<DomainException>(() => SearchAsync(
            new PublicListingFieldFilter(yearField, Min: 2025, Max: 2020)));
    }

    [Fact]
    public async Task FiltersCombineWithTextSearchAndPrice()
    {
        await EnsureSeededAsync();

        await using RentoXDbContext db = database.CreateContext();

        PagedResult<PublicListingSummaryResult> page =
            await new PublicListingQueryService(
                db, new FixedClock(), new NoViewRecorder())
            .SearchAsync(
                new PublicListingSearchQuery(
                    categoryId,
                    "toyota",
                    1,
                    20,
                    MaxPrice: 90,
                    FieldFilters:
                    [
                        new PublicListingFieldFilter(
                            gearboxField, [automatic])
                    ]),
                PreferredLanguage.Azerbaijani,
                null);

        Assert.Equal(
            ["Toyota auto 2022"],
            page.Items.Select(item => item.Title));
    }

    // ---- helpers ---------------------------------------------------------

    private async Task<List<string>> SearchAsync(
        params PublicListingFieldFilter[] filters)
    {
        await using RentoXDbContext db = database.CreateContext();

        PagedResult<PublicListingSummaryResult> page =
            await new PublicListingQueryService(
                db, new FixedClock(), new NoViewRecorder())
            .SearchAsync(
                new PublicListingSearchQuery(
                    categoryId,
                    null,
                    1,
                    20,
                    FieldFilters: filters),
                PreferredLanguage.Azerbaijani,
                null);

        return [.. page.Items.Select(item => item.Title)];
    }

    private async Task EnsureSeededAsync()
    {
        await SeedGate.WaitAsync();

        try
        {
            if (seeded)
            {
                return;
            }

            await using RentoXDbContext db = database.CreateContext();
            await SeedAsync(db);
            seeded = true;
        }
        finally
        {
            SeedGate.Release();
        }
    }

    private static async Task SeedAsync(RentoXDbContext db)
    {
        Guid ownerId = Guid.NewGuid();
        string userName = $"field-filter-{ownerId:N}";

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
            null, $"cars-{Guid.NewGuid():N}", null, 0);
        category.AddTranslation(
            CategoryTranslation.Create(
                category.Id, PreferredLanguage.Azerbaijani, "Avtomobil"));
        db.Categories.Add(category);
        categoryId = category.Id;

        CategoryField brand = NewField(category.Id, "brand", CategoryFieldType.SingleSelect);
        CategoryField gearbox = NewField(category.Id, "gearbox", CategoryFieldType.SingleSelect);
        CategoryField year = NewField(category.Id, "year", CategoryFieldType.WholeNumber);
        CategoryField electric = NewField(category.Id, "electric", CategoryFieldType.Boolean);

        CategoryFieldOption toyotaOption = AddOption(brand, "Toyota");
        CategoryFieldOption bmwOption = AddOption(brand, "BMW");
        CategoryFieldOption autoOption = AddOption(gearbox, "Avtomat");
        CategoryFieldOption manualOption = AddOption(gearbox, "Mexaniki");

        db.CategoryFields.AddRange(brand, gearbox, year, electric);

        brandField = brand.Id;
        gearboxField = gearbox.Id;
        yearField = year.Id;
        electricField = electric.Id;
        toyota = toyotaOption.Id;
        bmw = bmwOption.Id;
        automatic = autoOption.Id;
        manual = manualOption.Id;

        (string Title, Guid Brand, Guid Gearbox, int Year, bool Electric, decimal Price)[] rows =
        [
            ("Toyota auto 2022", toyota, automatic, 2022, true, 80m),
            ("Toyota manual 2018", toyota, manual, 2018, false, 50m),
            ("BMW auto 2021", bmw, automatic, 2021, false, 150m),
            ("BMW manual 2015", bmw, manual, 2015, false, 70m)
        ];

        foreach (var row in rows)
        {
            Listing listing = Listing.Create(
                ownerId,
                category.Id,
                row.Title,
                "Təsvir mətni burada yazılıb.",
                row.Price,
                "AZN",
                (RentalPeriodUnit)2);

            listing.MarkAsCreated(Now.AddDays(-30), ownerId);

            ListingFieldValue brandValue =
                ListingFieldValue.Create(listing.Id, brandField);
            brandValue.AddSelection(row.Brand);
            listing.AddFieldValue(brandValue);

            ListingFieldValue gearboxValue =
                ListingFieldValue.Create(listing.Id, gearboxField);
            gearboxValue.AddSelection(row.Gearbox);
            listing.AddFieldValue(gearboxValue);

            ListingFieldValue yearValue =
                ListingFieldValue.Create(listing.Id, yearField);
            yearValue.SetNumber(row.Year);
            listing.AddFieldValue(yearValue);

            ListingFieldValue electricValue =
                ListingFieldValue.Create(listing.Id, electricField);
            electricValue.SetFlag(row.Electric);
            listing.AddFieldValue(electricValue);

            db.Listings.Add(listing);

            db.Entry(listing).Property(item => item.Status)
                .CurrentValue = ListingStatus.Active;
            db.Entry(listing).Property(item => item.PublishedAtUtc)
                .CurrentValue = Now.AddDays(-20);
            db.Entry(listing).Property(item => item.ExpiresAtUtc)
                .CurrentValue = Now.AddDays(20);
        }

        await db.SaveChangesAsync();
    }

    private static CategoryField NewField(
        Guid categoryId,
        string key,
        CategoryFieldType type) =>
        CategoryField.Create(
            categoryId, key, type,
            isRequired: false,
            isFilterable: true,
            isSearchable: false,
            allowCustomValue: false,
            appliesToDescendants: true,
            displayOrder: 0);

    private static CategoryFieldOption AddOption(
        CategoryField field,
        string value)
    {
        CategoryFieldOption option =
            CategoryFieldOption.Create(field.Id, value, 0);
        field.AddOption(option);

        return option;
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class NoViewRecorder : IListingViewRecorder
    {
        public Task<long> RecordAsync(
            Guid listingId,
            Guid? viewerUserId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0L);
    }
}
