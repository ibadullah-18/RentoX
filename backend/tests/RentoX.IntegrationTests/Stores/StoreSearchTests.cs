using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Common;
using RentoX.Application.Listings;
using RentoX.Application.Stores;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Stores;
using RentoX.Domain.Stores.Enums;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Listings;
using RentoX.Infrastructure.Persistence;
using RentoX.Infrastructure.Stores;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Stores;

public sealed class StoreSearchTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

    private static readonly SemaphoreSlim SeedGate = new(1, 1);
    private static bool seeded;
    private static Guid carStoreListingId;
    private static Guid followerId;

    [Fact]
    public async Task WithoutSearchTheMostActiveStoresComeFirst()
    {
        await EnsureSeededAsync();

        List<string> names = await SearchNamesAsync(null);

        Assert.Equal("Avto Dünya", names[0]);
        Assert.Contains("Texno Mağaza", names);
        Assert.Contains("Boş Mağaza", names);
    }

    [Fact]
    public async Task OnlyActiveStoresAreListed()
    {
        await EnsureSeededAsync();

        List<string> names = await SearchNamesAsync(null);

        Assert.DoesNotContain("Qaralama Mağaza", names);
    }

    [Theory]
    [InlineData("avto")]
    [InlineData("AVTO")]
    [InlineData("avtoo")]
    [InlineData("dunya")]
    [InlineData("Dünya")]
    public async Task NameMatchingIgnoresCaseLettersAndSmallTypos(
        string typed)
    {
        await EnsureSeededAsync();

        List<string> names = await SearchNamesAsync(typed);

        Assert.Equal("Avto Dünya", names[0]);
    }

    [Fact]
    public async Task EveryWordMustMatch()
    {
        await EnsureSeededAsync();

        Assert.Equal(
            ["Avto Dünya"],
            await SearchNamesAsync("avto dunya"));
        Assert.Empty(await SearchNamesAsync("avto texno"));
    }

    [Fact]
    public async Task DescriptionIsSearchedButRanksBelowTheName()
    {
        await EnsureSeededAsync();

        List<string> names = await SearchNamesAsync("texno");

        // "Texno Mağaza" has it in its name; "Boş Mağaza" only mentions it
        // in the description.
        Assert.Equal("Texno Mağaza", names[0]);
        Assert.Contains("Boş Mağaza", names);
    }

    [Fact]
    public async Task NothingFoundGivesAnEmptyPage()
    {
        await EnsureSeededAsync();

        PagedResult<PublicStoreSummaryResult> page =
            await SearchAsync("zzzzqqq");

        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
        Assert.Equal(0, page.TotalPages);
    }

    [Fact]
    public async Task ResultsCarryCountsLogoAndPaging()
    {
        await EnsureSeededAsync();

        PagedResult<PublicStoreSummaryResult> page =
            await SearchAsync(null, pageSize: 2);

        Assert.Equal(2, page.Items.Count);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.TotalPages);

        PublicStoreSummaryResult first = page.Items[0];
        Assert.Equal("Avto Dünya", first.Name);
        Assert.Equal(2, first.ActiveListingCount);
        Assert.Equal(1, first.FollowerCount);
        Assert.True(first.HasLogoImage);

        PagedResult<PublicStoreSummaryResult> second =
            await SearchAsync(null, page: 2, pageSize: 2);

        Assert.Single(second.Items);
    }

    [Fact]
    public async Task InvalidPagingIsRejected()
    {
        await EnsureSeededAsync();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            SearchAsync(null, page: 0));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            SearchAsync(null, pageSize: 51));
    }

    [Fact]
    public async Task FallsBackToPlainMatchingWhenTheMigrationIsMissing()
    {
        await using RentoXDbContext db = database.CreateContext();
        await EnsureSeededAsync();

        await db.Database.ExecuteSqlRawAsync(
            "ALTER FUNCTION public.rentox_norm(text) RENAME TO rentox_norm_off;");

        try
        {
            PublicStoreQueryService service = CreateService(db);

            PagedResult<PublicStoreSummaryResult> page =
                await service.SearchAsync(
                    new PublicStoreSearchQuery("Texno"));

            Assert.Contains(page.Items, item => item.Name == "Texno Mağaza");
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync(
                "ALTER FUNCTION public.rentox_norm_off(text) RENAME TO rentox_norm;");
        }
    }

    [Fact]
    public async Task ListingDetailsIncludeTheOwnersActiveStore()
    {
        await EnsureSeededAsync();

        await using RentoXDbContext db = database.CreateContext();

        PublicListingQueryService listings =
            new(db, new FixedClock(), new NoViewRecorder());

        PublicListingDetailsResult? details =
            await listings.GetByIdAsync(
                carStoreListingId,
                PreferredLanguage.Azerbaijani,
                null);

        Assert.NotNull(details);
        Assert.NotNull(details.Owner.Store);
        Assert.Equal("Avto Dünya", details.Owner.Store.Name);
        Assert.Equal("avto-dunya", details.Owner.Store.Slug);
        Assert.True(details.Owner.Store.HasLogoImage);
    }

    [Fact]
    public async Task FollowedStoresAreListedWithCounts()
    {
        await EnsureSeededAsync();

        await using RentoXDbContext db = database.CreateContext();

        PagedResult<FollowedStoreResult> page =
            await new StoreFollowService(db, new FixedClock())
                .GetFollowingAsync(followerId, 1, 20);

        FollowedStoreResult store = Assert.Single(page.Items);
        Assert.Equal("Avto Dünya", store.Name);
        Assert.Equal(2, store.ActiveListingCount);
        Assert.Equal(1, store.FollowerCount);
        Assert.Equal(1, page.TotalCount);
        Assert.Equal(1, page.TotalPages);
    }

    [Fact]
    public async Task SellerTypeSeparatesStoresFromIndividuals()
    {
        await EnsureSeededAsync();

        List<string> stores = await ListingTitlesAsync(
            sellerType: PublicListingSellerType.Store);
        List<string> individuals = await ListingTitlesAsync(
            sellerType: PublicListingSellerType.Individual);

        Assert.Equal(3, stores.Count);
        Assert.DoesNotContain("Velosiped", stores);
        Assert.Equal(["Velosiped"], individuals);
        Assert.Equal(4, (await ListingTitlesAsync()).Count);
    }

    [Fact]
    public async Task PriceSortingOrdersTheWholeResult()
    {
        await EnsureSeededAsync();

        Assert.Equal(
            ["Velosiped", "Hyundai Elantra", "Toyota Camry", "Noutbuk"],
            await ListingTitlesAsync(sort: PublicListingSort.PriceAscending));
        Assert.Equal(
            ["Noutbuk", "Toyota Camry", "Hyundai Elantra", "Velosiped"],
            await ListingTitlesAsync(sort: PublicListingSort.PriceDescending));
    }

    [Fact]
    public async Task PriceSortingWorksTogetherWithTextSearchAndSellerType()
    {
        await EnsureSeededAsync();

        List<string> titles = await ListingTitlesAsync(
            search: "toyota",
            sort: PublicListingSort.PriceDescending,
            sellerType: PublicListingSellerType.Store);

        Assert.Equal(["Toyota Camry"], titles);
    }

    private async Task<List<string>> ListingTitlesAsync(
        string? search = null,
        PublicListingSort sort = PublicListingSort.Default,
        PublicListingSellerType sellerType = PublicListingSellerType.Any)
    {
        await using RentoXDbContext db = database.CreateContext();

        PagedResult<PublicListingSummaryResult> page =
            await new PublicListingQueryService(
                db, new FixedClock(), new NoViewRecorder())
            .SearchAsync(
                new PublicListingSearchQuery(
                    null,
                    search,
                    1,
                    20,
                    SellerType: sellerType,
                    Sort: sort),
                PreferredLanguage.Azerbaijani,
                null);

        return [.. page.Items.Select(item => item.Title)];
    }

    // ---- helpers ---------------------------------------------------------

    private async Task<List<string>> SearchNamesAsync(string? typed)
    {
        PagedResult<PublicStoreSummaryResult> page =
            await SearchAsync(typed);

        return [.. page.Items.Select(item => item.Name)];
    }

    private async Task<PagedResult<PublicStoreSummaryResult>> SearchAsync(
        string? typed,
        int page = 1,
        int pageSize = 20)
    {
        await using RentoXDbContext db = database.CreateContext();

        return await CreateService(db).SearchAsync(
            new PublicStoreSearchQuery(typed, page, pageSize));
    }

    private static PublicStoreQueryService CreateService(
        RentoXDbContext db) =>
        new(
            db,
            new FixedClock(),
            new PublicListingQueryService(
                db,
                new FixedClock(),
                new NoViewRecorder()));

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
        Category category = Category.Create(
            null,
            $"store-search-{Guid.NewGuid():N}",
            null,
            0);
        category.AddTranslation(
            CategoryTranslation.Create(
                category.Id,
                PreferredLanguage.Azerbaijani,
                "Avtomobil"));
        db.Categories.Add(category);

        AppUser carOwner = NewUser(db, "car-owner");
        AppUser techOwner = NewUser(db, "tech-owner");
        AppUser emptyOwner = NewUser(db, "empty-owner");
        AppUser draftOwner = NewUser(db, "draft-owner");
        AppUser follower = NewUser(db, "follower");
        followerId = follower.Id;

        StoreProfile carStore = NewStore(
            db, carOwner.Id, "Avto Dünya", "avto-dunya",
            "Ən yaxşı avtomobillər burada kirayə verilir.",
            StoreStatus.Active);
        NewStore(
            db, techOwner.Id, "Texno Mağaza", "texno-magaza",
            "Elektronika və aksesuarlar.",
            StoreStatus.Active);
        NewStore(
            db, emptyOwner.Id, "Boş Mağaza", "bos-magaza",
            "Texno aləmindən yeni gələn mağaza.",
            StoreStatus.Active);
        NewStore(
            db, draftOwner.Id, "Qaralama Mağaza", "qaralama",
            "Hələ təsdiqlənməyib, avto yoxdur.",
            StoreStatus.Draft);

        AppUser individual = NewUser(db, "individual");

        carStoreListingId = NewListing(
            db, carOwner.Id, category.Id, "Toyota Camry", 80m);
        NewListing(db, carOwner.Id, category.Id, "Hyundai Elantra", 60m);
        NewListing(db, techOwner.Id, category.Id, "Noutbuk", 120m);
        NewListing(db, individual.Id, category.Id, "Velosiped", 30m);

        db.StoreFollowers.Add(
            StoreFollower.Create(follower.Id, carStore.Id, Now));

        await db.SaveChangesAsync();
    }

    private static AppUser NewUser(RentoXDbContext db, string prefix)
    {
        Guid id = Guid.NewGuid();
        string userName = $"{prefix}-{id:N}";

        AppUser user = new()
        {
            Id = id,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            RegisteredAtUtc = Now,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };

        db.Users.Add(user);

        return user;
    }

    private static StoreProfile NewStore(
        RentoXDbContext db,
        Guid ownerId,
        string name,
        string slug,
        string description,
        StoreStatus status)
    {
        StoreProfile store = StoreProfile.Create(
            ownerId,
            name,
            slug,
            description,
            "+994500000000",
            null,
            null,
            null,
            null,
            null,
            null);

        store.MarkAsCreated(Now.AddDays(-10), ownerId);
        store.SetLogo("store-logos/" + Guid.NewGuid().ToString("N") + ".png");

        db.StoreProfiles.Add(store);

        db.Entry(store).Property(item => item.Status)
            .CurrentValue = status;

        return store;
    }

    private static Guid NewListing(
        RentoXDbContext db,
        Guid ownerId,
        Guid categoryId,
        string title,
        decimal price)
    {
        Listing listing = Listing.Create(
            ownerId,
            categoryId,
            title,
            "Təsvir mətni burada yazılıb.",
            price,
            "AZN",
            (RentalPeriodUnit)2);

        listing.MarkAsCreated(Now.AddDays(-5), ownerId);
        db.Listings.Add(listing);

        db.Entry(listing).Property(item => item.Status)
            .CurrentValue = ListingStatus.Active;
        db.Entry(listing).Property(item => item.PublishedAtUtc)
            .CurrentValue = Now.AddDays(-4);
        db.Entry(listing).Property(item => item.ExpiresAtUtc)
            .CurrentValue = Now.AddDays(20);

        return listing.Id;
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
