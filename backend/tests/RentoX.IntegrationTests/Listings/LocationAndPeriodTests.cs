using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Catalog.Categories;
using RentoX.Application.Common;
using RentoX.Application.Listings;
using RentoX.Application.Locations;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Locations;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Catalog.Categories;
using RentoX.Infrastructure.Catalog.Fields;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Listings;
using RentoX.Infrastructure.Locations;
using RentoX.Infrastructure.Persistence;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Listings;

public sealed class LocationAndPeriodTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 11, 12, 0, 0, TimeSpan.Zero);

    private static readonly SemaphoreSlim SeedGate = new(1, 1);
    private static bool seeded;

    private static Guid ownerId;
    private static Guid rentals, apartments, cars;
    private static Guid baku, sumgait, nasimi, yasamal, ganjaKapaz;

    // ---- reference data ---------------------------------------------------

    [Fact]
    public async Task CitiesAreSeededAndBakuHasItsDistricts()
    {
        await EnsureSeededAsync();
        await using RentoXDbContext db = database.CreateContext();
        LocationQueryService service = new(db);

        IReadOnlyList<CityResult> cities =
            await service.GetCitiesAsync(PreferredLanguage.Azerbaijani);

        Assert.True(cities.Count >= 20);
        CityResult bakuCity = Assert.Single(cities, c => c.Name == "Bakı");
        Assert.True(bakuCity.HasDistricts);
        Assert.False(
            Assert.Single(cities, c => c.Name == "Sumqayıt").HasDistricts);

        IReadOnlyList<DistrictResult>? districts =
            await service.GetDistrictsAsync(
                bakuCity.Id, PreferredLanguage.Azerbaijani);
        Assert.NotNull(districts);
        Assert.Equal(12, districts.Count);
        Assert.Contains(districts, d => d.Name == "Nərimanov");

        // Names follow the language.
        Assert.Contains(
            await service.GetCitiesAsync(PreferredLanguage.English),
            c => c.Name == "Baku");
        Assert.Null(await service.GetDistrictsAsync(
            Guid.NewGuid(), PreferredLanguage.Azerbaijani));
    }

    // ---- creating ---------------------------------------------------------

    [Fact]
    public async Task ANewListingNeedsACity()
    {
        await EnsureSeededAsync();

        DomainException error = await Assert.ThrowsAsync<DomainException>(
            () => CreateAsync(apartments, 4, cityId: null, districtId: null));

        Assert.Contains("City is required", error.Message);
    }

    [Fact]
    public async Task ACityWithDistrictsNeedsOne()
    {
        await EnsureSeededAsync();

        DomainException error = await Assert.ThrowsAsync<DomainException>(
            () => CreateAsync(apartments, 4, baku, districtId: null));
        Assert.Contains("District is required", error.Message);

        // A city without districts is fine on its own.
        CreateListingResult ok = await CreateAsync(apartments, 4, sumgait, null);
        Assert.NotEqual(Guid.Empty, ok.Id);
    }

    [Fact]
    public async Task TheDistrictMustBelongToTheCity()
    {
        await EnsureSeededAsync();

        await Assert.ThrowsAsync<DomainException>(
            () => CreateAsync(apartments, 4, baku, ganjaKapaz));
        await Assert.ThrowsAsync<DomainException>(
            () => CreateAsync(apartments, 4, sumgait, nasimi));
        await Assert.ThrowsAsync<DomainException>(
            () => CreateAsync(apartments, 4, Guid.NewGuid(), null));
    }

    [Fact]
    public async Task TheLocationIsStoredAndShownWithTheListing()
    {
        await EnsureSeededAsync();

        CreateListingResult created = await CreateAsync(
            apartments, 4, baku, nasimi);

        await using RentoXDbContext db = database.CreateContext();
        Listing listing = await db.Listings.SingleAsync(l => l.Id == created.Id);
        Assert.Equal(baku, listing.CityId);
        Assert.Equal(nasimi, listing.DistrictId);

        // Owner view (for the edit form).
        OwnedListingDetailsResult? owned =
            await new ListingQueryService(db)
                .GetMineByIdAsync(
                    ownerId, created.Id, PreferredLanguage.Azerbaijani);
        Assert.Equal("Bakı", owned!.CityName);
        Assert.Equal("Nəsimi", owned.DistrictName);
    }

    // ---- rental periods per category ------------------------------------------

    [Fact]
    public async Task ACategoryOnlyAcceptsItsRentalPeriods()
    {
        await EnsureSeededAsync();

        // Apartments: day (2), week (3), month (4).
        await CreateAsync(apartments, 2, sumgait, null);
        await CreateAsync(apartments, 4, sumgait, null);

        DomainException hour = await Assert.ThrowsAsync<DomainException>(
            () => CreateAsync(apartments, 1, sumgait, null));
        Assert.Contains("not allowed", hour.Message);
        await Assert.ThrowsAsync<DomainException>(
            () => CreateAsync(apartments, 5, sumgait, null));
    }

    [Fact]
    public async Task ASubcategoryInheritsAndEveryPeriodIsOkWhenNothingIsSet()
    {
        await EnsureSeededAsync();

        // "cars" has no choice of its own, its parent "rentals" allows
        // hour, day and week.
        await CreateAsync(cars, 1, sumgait, null);
        await CreateAsync(cars, 3, sumgait, null);
        await Assert.ThrowsAsync<DomainException>(
            () => CreateAsync(cars, 4, sumgait, null));
    }

    [Fact]
    public async Task TheCategoryTreeTellsTheEffectivePeriods()
    {
        await EnsureSeededAsync();
        await using RentoXDbContext db = database.CreateContext();

        IReadOnlyList<CategoryTreeResult> tree =
            await new CategoryQueryService(db)
                .GetTreeAsync(PreferredLanguage.Azerbaijani);

        CategoryTreeResult top = Assert.Single(
            tree, c => c.Id == rentals);
        Assert.Equal([1, 2, 3], top.RentalPeriods);
        Assert.Equal([1, 2, 3], top.Children
            .Single(c => c.Id == cars).RentalPeriods); // inherited
        Assert.Equal([2, 3, 4], top.Children
            .Single(c => c.Id == apartments).RentalPeriods);
    }

    [Fact]
    public async Task AdminCanSetAndClearTheAllowedPeriods()
    {
        await EnsureSeededAsync();
        await using RentoXDbContext db = database.CreateContext();
        CategoryManagementService service = new(db);

        Category extra = Category.Create(
            null, $"extra-{Guid.NewGuid():N}", null, 0);
        extra.AddTranslation(
            CategoryTranslation.Create(
                extra.Id, PreferredLanguage.Azerbaijani, "Extra"));
        db.Categories.Add(extra);
        await db.SaveChangesAsync();

        await service.SetRentalPeriodsAsync(extra.Id, [2, 4]);
        Assert.Equal(
            [2, 4],
            Category.PeriodsOf(
                (await db.Categories.AsNoTracking()
                    .SingleAsync(c => c.Id == extra.Id)).RentalPeriodMask));

        await Assert.ThrowsAsync<DomainException>(
            () => service.SetRentalPeriodsAsync(extra.Id, [9]));
        await service.SetRentalPeriodsAsync(extra.Id, []);
        Assert.Equal(
            0,
            (await db.Categories.AsNoTracking()
                .SingleAsync(c => c.Id == extra.Id)).RentalPeriodMask);
        await Assert.ThrowsAsync<DomainException>(
            () => service.SetRentalPeriodsAsync(Guid.NewGuid(), [2]));
    }

    // ---- searching ------------------------------------------------------------

    [Fact]
    public async Task SearchCanFilterByCityDistrictAndPeriod()
    {
        await EnsureSeededAsync();

        PagedResult<PublicListingSummaryResult> inBaku =
            await SearchAsync(cityId: baku);
        Assert.All(inBaku.Items, i => Assert.StartsWith("Bakı", i.Location));
        Assert.Contains(inBaku.Items, i => i.Title == "Baku Nasimi flat");
        Assert.DoesNotContain(inBaku.Items, i => i.Title == "Sumgait flat");

        PagedResult<PublicListingSummaryResult> nasimiOnly =
            await SearchAsync(cityId: baku, districtId: nasimi);
        Assert.Equal(
            ["Baku Nasimi flat"],
            nasimiOnly.Items.Select(i => i.Title));
        Assert.Equal("Bakı, Nəsimi", nasimiOnly.Items[0].Location);

        PagedResult<PublicListingSummaryResult> sumgaitOnly =
            await SearchAsync(cityId: sumgait);
        Assert.Equal("Sumqayıt", sumgaitOnly.Items[0].Location);

        PagedResult<PublicListingSummaryResult> monthly =
            await SearchAsync(period: 4);
        Assert.All(monthly.Items, i => Assert.Equal(4, i.RentalPeriodUnit));
        Assert.True(monthly.TotalCount >= 2);

        await Assert.ThrowsAsync<DomainException>(
            () => SearchAsync(period: 99));
    }

    [Fact]
    public async Task ListingDetailsCarryTheLocationNames()
    {
        await EnsureSeededAsync();
        await using RentoXDbContext db = database.CreateContext();

        Guid id = await db.Listings
            .Where(l => l.Title == "Baku Nasimi flat")
            .Select(l => l.Id)
            .SingleAsync();

        PublicListingDetailsResult? details =
            await new PublicListingQueryService(
                db, new FixedClock(), new NoViewRecorder())
            .GetByIdAsync(id, PreferredLanguage.Azerbaijani, null);

        Assert.Equal("Bakı", details!.CityName);
        Assert.Equal("Nəsimi", details.DistrictName);
        Assert.Equal(baku, details.CityId);
    }

    // ---- helpers ---------------------------------------------------------------

    private async Task<CreateListingResult> CreateAsync(
        Guid categoryId,
        int unit,
        Guid? cityId,
        Guid? districtId)
    {
        await using RentoXDbContext db = database.CreateContext();

        return await new ListingCreationService(
                db, new CategoryFieldQueryService(db))
            .CreateAsync(new CreateListingCommand(
                ownerId,
                categoryId,
                "Created listing",
                "Təsvir mətni burada yazılıb.",
                100m,
                "AZN",
                unit,
                [],
                cityId,
                districtId));
    }

    private async Task<PagedResult<PublicListingSummaryResult>> SearchAsync(
        Guid? cityId = null,
        Guid? districtId = null,
        int? period = null)
    {
        await using RentoXDbContext db = database.CreateContext();

        return await new PublicListingQueryService(
                db, new FixedClock(), new NoViewRecorder())
            .SearchAsync(
                new PublicListingSearchQuery(
                    null, null, 1, 50,
                    CityId: cityId,
                    DistrictId: districtId,
                    RentalPeriodUnit: period),
                PreferredLanguage.Azerbaijani,
                null);
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
        ownerId = Guid.NewGuid();
        string userName = $"loc-{ownerId:N}";
        db.Users.Add(new AppUser
        {
            Id = ownerId,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            RegisteredAtUtc = Now,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        });

        Category top = NewCategory(null, "rentals");
        Category apt = NewCategory(top.Id, "apartments");
        Category car = NewCategory(top.Id, "cars");
        top.SetRentalPeriods([1, 2, 3]);
        apt.SetRentalPeriods([2, 3, 4]);
        db.Categories.AddRange(top, apt, car);
        rentals = top.Id;
        apartments = apt.Id;
        cars = car.Id;

        baku = await db.Cities.Where(c => c.NameAz == "Bakı")
            .Select(c => c.Id).SingleAsync();
        sumgait = await db.Cities.Where(c => c.NameAz == "Sumqayıt")
            .Select(c => c.Id).SingleAsync();
        nasimi = await db.Districts.Where(d => d.NameAz == "Nəsimi")
            .Select(d => d.Id).SingleAsync();
        yasamal = await db.Districts.Where(d => d.NameAz == "Yasamal")
            .Select(d => d.Id).SingleAsync();
        ganjaKapaz = await db.Districts.Where(d => d.NameAz == "Kəpəz")
            .Select(d => d.Id).SingleAsync();

        AddActive(db, "Baku Nasimi flat", apt.Id, 4, baku, nasimi);
        AddActive(db, "Baku Yasamal flat", apt.Id, 4, baku, yasamal);
        AddActive(db, "Sumgait flat", apt.Id, 2, sumgait, null);
        AddActive(db, "Old flat without place", apt.Id, 3, null, null);

        await db.SaveChangesAsync();
    }

    private static Category NewCategory(Guid? parentId, string slug)
    {
        Category category = Category.Create(
            parentId, $"{slug}-{Guid.NewGuid():N}", null, 0);
        category.AddTranslation(
            CategoryTranslation.Create(
                category.Id, PreferredLanguage.Azerbaijani, slug));

        return category;
    }

    private static void AddActive(
        RentoXDbContext db,
        string title,
        Guid categoryId,
        int unit,
        Guid? cityId,
        Guid? districtId)
    {
        Listing listing = Listing.Create(
            ownerId, categoryId, title,
            "Təsvir mətni burada yazılıb.", 50m, "AZN",
            (RentalPeriodUnit)unit);

        listing.MarkAsCreated(Now.AddDays(-5), ownerId);

        if (cityId.HasValue)
        {
            listing.SetLocation(cityId.Value, districtId);
        }

        db.Listings.Add(listing);

        db.Entry(listing).Property(item => item.Status)
            .CurrentValue = ListingStatus.Active;
        db.Entry(listing).Property(item => item.PublishedAtUtc)
            .CurrentValue = Now.AddDays(-4);
        db.Entry(listing).Property(item => item.ExpiresAtUtc)
            .CurrentValue = Now.AddDays(20);
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
