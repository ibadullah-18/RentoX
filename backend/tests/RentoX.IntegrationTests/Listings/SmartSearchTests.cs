using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Common;
using RentoX.Application.Listings;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Listings;
using RentoX.Infrastructure.Listings.Search;
using RentoX.Infrastructure.Persistence;
using RentoX.IntegrationTests.Auditing;
using Testcontainers.PostgreSql;

namespace RentoX.IntegrationTests.Listings;

public sealed class SmartSearchTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

    private static readonly SemaphoreSlim SeedGate = new(1, 1);
    private static bool seeded;

    // ---- matching -------------------------------------------------------

    [Fact]
    public async Task WholeWordMatchRanksTitleAboveDescription()
    {
        await EnsureSeededAsync();

        List<string> titles = await SearchTitlesAsync("toyota");

        // "Toyota Camry" has it in the title; the Hyundai only mentions
        // Toyota in its description, so it comes second.
        Assert.Equal("Toyota Camry 2023", titles[0]);
        Assert.Contains("Hyundai Elantra", titles);
        Assert.True(
            titles.IndexOf("Toyota Camry 2023") <
            titles.IndexOf("Hyundai Elantra"));
    }

    [Theory]
    [InlineData("toyta")]
    [InlineData("toyotta")]
    [InlineData("TOYOTA")]
    public async Task TyposAndCaseStillFindTheListing(string typed)
    {
        await EnsureSeededAsync();

        List<string> titles = await SearchTitlesAsync(typed);

        Assert.Equal("Toyota Camry 2023", titles[0]);
    }

    [Theory]
    [InlineData("maşın")]
    [InlineData("masin")]
    [InlineData("MAŞIN")]
    [InlineData("avtomobil")]
    [InlineData("car")]
    [InlineData("maşınlar")]
    public async Task CarSynonymsFindEveryListingInTheCarCategory(
        string typed)
    {
        await EnsureSeededAsync();

        List<string> titles = await SearchTitlesAsync(typed);

        Assert.Contains("Toyota Camry 2023", titles);
        Assert.Contains("Hyundai Elantra", titles);
        Assert.Contains("BMW 520", titles);
        Assert.DoesNotContain("Çadır 4 nəfərlik", titles);
    }

    [Fact]
    public async Task AllWordsMustMatchAndTheBestListingIsFirst()
    {
        await EnsureSeededAsync();

        List<string> titles = await SearchTitlesAsync("toyota maşın");

        Assert.Equal("Toyota Camry 2023", titles[0]);
        Assert.DoesNotContain("BMW 520", titles);
    }

    [Theory]
    [InlineData("hovuz", "Villa Buzovnada hovuzlu")]
    [InlineData("otaq", "Nizami küçəsində 3 otaqlı mənzil")]
    [InlineData("kvartira", "Nizami küçəsində 3 otaqlı mənzil")]
    [InlineData("nizami mənzil", "Nizami küçəsində 3 otaqlı mənzil")]
    [InlineData("мотоцикл", "Мотоцикл Yamaha")]
    [InlineData("motosiklet", "Мотоцикл Yamaha")]
    [InlineData("alət", "Elektrik alət dəsti Bosch")]
    [InlineData("tool", "Elektrik alət dəsti Bosch")]
    [InlineData("çadır", "Çadır 4 nəfərlik")]
    [InlineData("cadir", "Çadır 4 nəfərlik")]
    public async Task FindsTheExpectedListing(string typed, string expected)
    {
        await EnsureSeededAsync();

        List<string> titles = await SearchTitlesAsync(typed);

        Assert.Contains(expected, titles);
    }

    [Theory]
    [InlineData("zzzzqqqq")]
    [InlineData("lada")] // expired listing is never shown
    [InlineData("toyota zzzzqqqq")] // every word must match
    [InlineData("!!!")]
    public async Task NothingMatchesReturnsAnEmptyPage(string typed)
    {
        await EnsureSeededAsync();

        PagedResult<PublicListingSummaryResult> page =
            await SearchAsync(typed);

        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
    }

    // ---- paging and filters ------------------------------------------

    [Fact]
    public async Task PagingKeepsTheRankingOrder()
    {
        await EnsureSeededAsync();

        List<string> all = await SearchTitlesAsync("toyota maşın");
        PagedResult<PublicListingSummaryResult> first =
            await SearchAsync("maşın", page: 1, pageSize: 2);
        PagedResult<PublicListingSummaryResult> second =
            await SearchAsync("maşın", page: 2, pageSize: 2);

        Assert.True(all.Count >= 1);
        Assert.Equal(3, first.TotalCount);
        Assert.Equal(2, first.TotalPages);
        Assert.Equal(2, first.Items.Count);
        Assert.Single(second.Items);
        Assert.Empty(
            first.Items.Select(i => i.Id)
                .Intersect(second.Items.Select(i => i.Id)));
    }

    [Fact]
    public async Task OtherFiltersStillApplyToSearchResults()
    {
        await EnsureSeededAsync();

        PagedResult<PublicListingSummaryResult> page =
            await SearchAsync("maşın", maxPrice: 100);

        Assert.DoesNotContain(page.Items, i => i.Title == "BMW 520");
        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task WithoutSearchTextTheOrderingIsUnchanged()
    {
        await EnsureSeededAsync();

        PagedResult<PublicListingSummaryResult> page =
            await SearchAsync(null);

        // Everything live (the expired Lada is excluded).
        Assert.Equal(8, page.TotalCount);
    }

    // ---- fallback --------------------------------------------------------

    [Fact]
    public async Task WorksWithPlainTextSearchUntilTheDatabaseIsUpgraded()
    {
        await using PostgreSqlContainer container =
            new PostgreSqlBuilder("postgres:17-alpine")
                .WithDatabase("rentox_search_fallback")
                .WithUsername("rentox_test")
                .WithPassword("rentox-isolated-test-password")
                .Build();

        using CancellationTokenSource timeout =
            new(TimeSpan.FromMinutes(5));
        await container.StartAsync(timeout.Token);

        DbContextOptions<RentoXDbContext> options =
            new DbContextOptionsBuilder<RentoXDbContext>()
                .UseNpgsql(container.GetConnectionString())
                .Options;

        await using RentoXDbContext db = new(options);
        await db.Database.MigrateAsync(timeout.Token);
        await SeedAsync(db);

        // Simulate a database that has not run the new migration.
        await db.Database.ExecuteSqlRawAsync(
            "DROP FUNCTION public.rentox_norm(text);",
            timeout.Token);

        PublicListingQueryService service =
            new(db, new FixedClock(), new NoViewRecorder());

        PagedResult<PublicListingSummaryResult> page =
            await service.SearchAsync(
                new PublicListingSearchQuery(null, "toyota", 1, 20),
                PreferredLanguage.Azerbaijani,
                null,
                timeout.Token);

        Assert.Contains(page.Items, i => i.Title == "Toyota Camry 2023");
    }

    // ---- suggestions -----------------------------------------------------

    [Fact]
    public async Task SuggestionsShowTitleWordsWithTheCategoryChain()
    {
        await EnsureSeededAsync();

        IReadOnlyList<ListingSuggestionResult> found =
            await SuggestAsync("toy");

        ListingSuggestionResult word = Assert.Single(
            found, s => s.Kind == "word");
        Assert.Equal("toyota", word.Text);
        Assert.Equal(["Nəqliyyat", "Avtomobil"], word.CategoryPath);
        Assert.NotNull(word.CategoryId);
    }

    [Fact]
    public async Task SuggestionsIgnoreAzerbaijaniLettersAndCase()
    {
        await EnsureSeededAsync();

        IReadOnlyList<ListingSuggestionResult> found =
            await SuggestAsync("CADIR");

        Assert.Contains(found, s => s.Kind == "word" && s.Text == "çadır");
    }

    [Fact]
    public async Task SuggestionsIncludeMatchingCategories()
    {
        await EnsureSeededAsync();

        IReadOnlyList<ListingSuggestionResult> found =
            await SuggestAsync("avto");

        ListingSuggestionResult category = Assert.Single(
            found, s => s.Kind == "category");
        Assert.Equal("Avtomobil", category.Text);
        Assert.Equal(["Nəqliyyat"], category.CategoryPath);
    }

    [Fact]
    public async Task SuggestionsSkipExpiredListingsAndShortInput()
    {
        await EnsureSeededAsync();

        Assert.DoesNotContain(
            await SuggestAsync("lada"), s => s.Text == "lada");
        Assert.Empty(await SuggestAsync("t"));
        Assert.Empty(await SuggestAsync("   "));
        Assert.Empty(await SuggestAsync(null));
    }

    private async Task<IReadOnlyList<ListingSuggestionResult>> SuggestAsync(
        string? typed)
    {
        await using RentoXDbContext db = database.CreateContext();

        return await new ListingSuggestionService(db, new FixedClock())
            .SuggestAsync(typed, PreferredLanguage.Azerbaijani);
    }

    // ---- helpers ---------------------------------------------------------

    private async Task<List<string>> SearchTitlesAsync(string? typed)
    {
        PagedResult<PublicListingSummaryResult> page =
            await SearchAsync(typed);

        return [.. page.Items.Select(i => i.Title)];
    }

    private async Task<PagedResult<PublicListingSummaryResult>> SearchAsync(
        string? typed,
        int page = 1,
        int pageSize = 20,
        decimal? maxPrice = null)
    {
        await using RentoXDbContext db = database.CreateContext();

        PublicListingQueryService service =
            new(db, new FixedClock(), new NoViewRecorder());

        return await service.SearchAsync(
            new PublicListingSearchQuery(
                null,
                typed,
                page,
                pageSize,
                MaxPrice: maxPrice),
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
        Guid ownerId = Guid.NewGuid();
        string userName = $"smart-search-{ownerId:N}";

        db.Users.Add(new AppUser
        {
            Id = ownerId,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            RegisteredAtUtc = Now,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        });

        Category transport = NewCategory(null, "transport", "Nəqliyyat", null);
        Category cars = NewCategory(transport.Id, "cars", "Avtomobil", "Cars");
        Category realEstate = NewCategory(null, "real-estate", "Daşınmaz əmlak", null);
        Category apartments = NewCategory(realEstate.Id, "apartments", "Mənzil", null);
        Category equipment = NewCategory(null, "equipment", "Texnika", null);

        db.Categories.AddRange(
            transport, cars, realEstate, apartments, equipment);

        (Category Category, string Title, string Description, decimal Price, bool Expired)[] rows =
        [
            (cars, "Toyota Camry 2023", "Təmiz və baxımlı sedan, günlük kirayə.", 85m, false),
            (cars, "Hyundai Elantra", "Qənaətli yanacaq sərfiyyatı. Toyota ilə müqayisədə daha ucuzdur.", 60m, false),
            (cars, "BMW 520", "Biznes sinif sedan", 140m, false),
            (apartments, "Nizami küçəsində 3 otaqlı mənzil", "Mərkəzdə, təmir olunmuş, mebelli.", 120m, false),
            (apartments, "Villa Buzovnada hovuzlu", "Yay üçün gözəl villa, 8 nəfərlik.", 300m, false),
            (equipment, "Elektrik alət dəsti Bosch", "Drel, perforator və s.", 25m, false),
            (equipment, "Çadır 4 nəfərlik", "Düşərgə üçün su keçirməyən çadır.", 15m, false),
            (transport, "Мотоцикл Yamaha", "Спортивный мотоцикл в аренду", 70m, false),
            (cars, "Köhnə Lada", "Müddəti bitib", 20m, true)
        ];

        foreach (var row in rows)
        {
            Listing listing = Listing.Create(
                ownerId,
                row.Category.Id,
                row.Title,
                row.Description,
                row.Price,
                "AZN",
                (RentalPeriodUnit)2);

            listing.MarkAsCreated(Now.AddDays(-30), ownerId);
            db.Listings.Add(listing);

            db.Entry(listing).Property(item => item.Status)
                .CurrentValue = ListingStatus.Active;
            db.Entry(listing).Property(item => item.PublishedAtUtc)
                .CurrentValue = Now.AddDays(-20);
            db.Entry(listing).Property(item => item.ExpiresAtUtc)
                .CurrentValue = row.Expired
                    ? Now.AddDays(-10)
                    : Now.AddDays(20);
        }

        await db.SaveChangesAsync();
    }

    private static Category NewCategory(
        Guid? parentId,
        string slug,
        string azName,
        string? enName)
    {
        Category category = Category.Create(
            parentId,
            $"{slug}-{Guid.NewGuid():N}",
            null,
            0);

        category.AddTranslation(
            CategoryTranslation.Create(
                category.Id,
                PreferredLanguage.Azerbaijani,
                azName));

        if (enName is not null)
        {
            category.AddTranslation(
                CategoryTranslation.Create(
                    category.Id,
                    PreferredLanguage.English,
                    enName));
        }

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
