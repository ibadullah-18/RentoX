using RentoX.Application.Abstractions.Time;
using RentoX.Application.Listings;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Catalog.Fields;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Listings;
using RentoX.Infrastructure.Persistence;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Listings;

public sealed class SimilarListingsTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

    private static readonly SemaphoreSlim SeedGate = new(1, 1);
    private static bool seeded;

    private static readonly Dictionary<string, Guid> Ids = [];
    private static Guid ownerA, ownerB;

    [Fact]
    public async Task SameBrandComesFirstThenTheRestOfTheCategoryThenNeighbours()
    {
        await EnsureSeededAsync();

        IReadOnlyList<PublicListingSummaryResult> similar =
            await SimilarAsync("Toyota 1", 6);

        List<string> titles = [.. similar.Select(item => item.Title)];

        Assert.DoesNotContain("Toyota 1", titles); // never itself
        Assert.Equal(titles.Count, titles.Distinct().Count());
        Assert.Equal(6, titles.Count);

        // About half is the same brand, listed before anything else.
        Assert.All(
            titles.Take(3),
            title => Assert.StartsWith("Toyota", title));

        // Then BMWs (same category), then motorcycles (neighbouring).
        Assert.Contains("BMW 1", titles);
        Assert.Contains(titles, title => title.StartsWith("Moto", StringComparison.Ordinal));
    }

    [Fact]
    public async Task OneSellerDoesNotTakeOverTheList()
    {
        await EnsureSeededAsync();

        IReadOnlyList<PublicListingSummaryResult> similar =
            await SimilarAsync("Toyota 1", 4);

        // Owner A has three more Toyotas, but at most two are shown while
        // other sellers have matching listings.
        Assert.True(
            similar.Count(item => item.OwnerId == ownerA) <= 2,
            "a seller should not fill the section");
    }

    [Fact]
    public async Task ASourceThatIsNotAvailableGivesNothing()
    {
        await EnsureSeededAsync();

        await using RentoXDbContext db = database.CreateContext();

        IReadOnlyList<PublicListingSummaryResult> none =
            await new PublicListingQueryService(
                db, new FixedClock(), new NoViewRecorder())
            .GetSimilarAsync(
                Guid.NewGuid(),
                PreferredLanguage.Azerbaijani,
                null,
                6);

        Assert.Empty(none);
    }

    [Fact]
    public async Task ASmallCategoryStillFillsUpFromElsewhere()
    {
        await EnsureSeededAsync();

        // Only one BMW-free motorcycle brand exists: the rest comes from
        // the neighbouring categories (cars) with the seller cap relaxed.
        IReadOnlyList<PublicListingSummaryResult> similar =
            await SimilarAsync("Moto 1", 10);

        Assert.True(similar.Count >= 5);
        Assert.DoesNotContain(similar, item => item.Title == "Moto 1");
    }

    // ---- helpers ---------------------------------------------------------

    private async Task<IReadOnlyList<PublicListingSummaryResult>>
        SimilarAsync(string title, int limit)
    {
        await using RentoXDbContext db = database.CreateContext();

        return await new PublicListingQueryService(
            db, new FixedClock(), new NoViewRecorder())
            .GetSimilarAsync(
                Ids[title],
                PreferredLanguage.Azerbaijani,
                null,
                limit);
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
        ownerA = NewUser(db, "similar-a");
        ownerB = NewUser(db, "similar-b");

        Category transport = NewCategory(null, "transport", "Nəqliyyat");
        Category cars = NewCategory(transport.Id, "cars", "Avtomobil");
        Category motos = NewCategory(transport.Id, "motos", "Motosiklet");
        db.Categories.AddRange(transport, cars, motos);

        CategoryField brand = CategoryField.Create(
            cars.Id, "brand", CategoryFieldType.SingleSelect,
            isRequired: false,
            isFilterable: true,
            isSearchable: false,
            allowCustomValue: true,
            appliesToDescendants: false,
            displayOrder: 1);
        db.CategoryFields.Add(brand);

        (string Title, Category Category, Guid Owner, string? Brand)[] rows =
        [
            ("Toyota 1", cars, ownerA, "Toyota"),
            ("Toyota 2", cars, ownerA, "toyota"),
            ("Toyota 3", cars, ownerA, "Toyota"),
            ("Toyota 4", cars, ownerB, "TOYOTA"),
            ("BMW 1", cars, ownerB, "BMW"),
            ("BMW 2", cars, ownerA, "BMW"),
            ("Moto 1", motos, ownerB, null),
            ("Moto 2", motos, ownerB, null)
        ];

        foreach (var row in rows)
        {
            Listing listing = Listing.Create(
                row.Owner,
                row.Category.Id,
                row.Title,
                "Təsvir mətni burada yazılıb.",
                50m,
                "AZN",
                (RentalPeriodUnit)2);

            listing.MarkAsCreated(Now.AddDays(-30), row.Owner);

            if (row.Brand is not null)
            {
                ListingFieldValue value =
                    ListingFieldValue.Create(listing.Id, brand.Id);
                value.SetCustomValue(row.Brand);
                listing.AddFieldValue(value);
            }

            db.Listings.Add(listing);

            db.Entry(listing).Property(item => item.Status)
                .CurrentValue = ListingStatus.Active;
            db.Entry(listing).Property(item => item.PublishedAtUtc)
                .CurrentValue = Now.AddDays(-20);
            db.Entry(listing).Property(item => item.ExpiresAtUtc)
                .CurrentValue = Now.AddDays(20);

            Ids[row.Title] = listing.Id;
        }

        await db.SaveChangesAsync();
    }

    private static Guid NewUser(RentoXDbContext db, string prefix)
    {
        Guid id = Guid.NewGuid();
        string userName = $"{prefix}-{id:N}";

        db.Users.Add(new AppUser
        {
            Id = id,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            RegisteredAtUtc = Now,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        });

        return id;
    }

    private static Category NewCategory(
        Guid? parentId,
        string slug,
        string azName)
    {
        Category category = Category.Create(
            parentId, $"{slug}-{Guid.NewGuid():N}", null, 0);
        category.AddTranslation(
            CategoryTranslation.Create(
                category.Id, PreferredLanguage.Azerbaijani, azName));

        return category;
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
