using RentoX.Application.Abstractions.Time;
using RentoX.Application.Common;
using RentoX.Application.Listings;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Listings.Promotions;
using RentoX.Domain.Users.Enums;
using RentoX.Domain.Wallets;
using RentoX.Domain.Wallets.Enums;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Listings;
using RentoX.Infrastructure.Persistence;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Listings;

public sealed class FeedOrderTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

    private static readonly SemaphoreSlim SeedGate = new(1, 1);
    private static bool seeded;

    [Fact]
    public void RoundsAlternateKindsAndSkipEmptyOnes()
    {
        ListingFeedInterleaver.Plan plan =
            ListingFeedInterleaver.Build(14, 6, 20, 0, 40);

        string pattern = string.Concat(plan.Page.Select(k => "VBP"[k]));

        Assert.Equal(
            "VVVVVV" + "BBBB" + "PPPPPP" +
            "VVVVVV" + "BB" + "PPPPPP" +
            "VV" + "PPPPPP" + "PP",
            pattern.Substring(0, 6 + 4 + 6 + 6 + 2 + 6 + 2 + 6 + 2));
        Assert.Equal(40, plan.Page.Count);
    }

    [Fact]
    public void SkipCountsWhatComesBeforeThePage()
    {
        // Page 2 of size 8 starts at position 8: after 6 VIP and 2 bumped.
        ListingFeedInterleaver.Plan plan =
            ListingFeedInterleaver.Build(14, 6, 20, 8, 8);

        Assert.Equal([6, 2, 0], plan.SkipPerKind);
        Assert.Equal(
            "BBPPPPPP",
            string.Concat(plan.Page.Select(k => "VBP"[k])));
        Assert.Equal([0, 2, 6], plan.TakePerKind);
    }

    [Fact]
    public void ANoVipFeedIsJustBumpedThenPlain()
    {
        ListingFeedInterleaver.Plan plan =
            ListingFeedInterleaver.Build(0, 3, 5, 0, 20);

        Assert.Equal(
            "BBBPPPPP",
            string.Concat(plan.Page.Select(k => "VBP"[k])));
    }

    [Fact]
    public async Task TheFeedMixesVipBumpedAndPlainListings()
    {
        await EnsureSeededAsync();

        List<string> first = await FeedAsync(1, 20);
        string pattern = string.Concat(first.Select(t => t[0]));

        // 6 VIP, 4 bumped, 6 plain, then VIP again.
        Assert.Equal("VVVVVV" + "BBBB" + "PPPPPP" + "VVVV", pattern);

        // Free listings are reachable on the very first page even though
        // there are more VIP listings than a page can hold.
        Assert.Contains(first, t => t.StartsWith('P'));
    }

    [Fact]
    public async Task PagingNeverRepeatsOrLosesAListing()
    {
        await EnsureSeededAsync();

        List<string> all = [];

        for (int page = 1; page <= 5; page++)
        {
            all.AddRange(await FeedAsync(page, 10));
        }

        Assert.Equal(40, all.Count);
        Assert.Equal(40, all.Distinct().Count());

        // Same order whatever the page size.
        Assert.Equal(all, await FeedAsync(1, 40));
    }

    // ---- helpers ---------------------------------------------------------

    private async Task<List<string>> FeedAsync(int page, int size)
    {
        await using RentoXDbContext db = database.CreateContext();

        PagedResult<PublicListingSummaryResult> result =
            await new PublicListingQueryService(
                db, new FixedClock(), new NoViewRecorder())
            .SearchAsync(
                new PublicListingSearchQuery(null, null, page, size),
                PreferredLanguage.Azerbaijani,
                null);

        Assert.Equal(40, result.TotalCount);

        return [.. result.Items.Select(item => item.Title)];
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
        string userName = $"feed-{ownerId:N}";

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
            null, $"feed-{Guid.NewGuid():N}", null, 0);
        category.AddTranslation(
            CategoryTranslation.Create(
                category.Id, PreferredLanguage.Azerbaijani, "Feed"));
        db.Categories.Add(category);

        // Promotions point at a real wallet transaction.
        Wallet wallet = Wallet.Create(ownerId);
        WalletTransaction Payment(int i) =>
            wallet.Credit(
                20m,
                WalletTransactionType.TopUp,
                "feed test",
                null,
                $"feed-test-{i}-" + Guid.NewGuid().ToString("N"),
                Now.AddDays(-2));
        db.Wallets.Add(wallet);

        // 14 VIP, 6 bumped, 20 plain. Titles start with V, B or P.
        for (int i = 0; i < 40; i++)
        {
            string kind = i < 14 ? "V" : i < 20 ? "B" : "P";
            Listing listing = Listing.Create(
                ownerId,
                category.Id,
                $"{kind}{i:00} listing",
                "Təsvir mətni burada yazılıb.",
                10m + i,
                "AZN",
                (RentalPeriodUnit)2);

            listing.MarkAsCreated(Now.AddDays(-30), ownerId);
            db.Listings.Add(listing);

            db.Entry(listing).Property(item => item.Status)
                .CurrentValue = ListingStatus.Active;
            db.Entry(listing).Property(item => item.PublishedAtUtc)
                .CurrentValue = Now.AddDays(-20).AddMinutes(i);
            db.Entry(listing).Property(item => item.ExpiresAtUtc)
                .CurrentValue = Now.AddDays(20);

            if (kind == "V")
            {
                db.ListingPromotions.Add(ListingPromotion.CreateVip(
                    listing.Id, 10m, Payment(i).Id,
                    Now.AddDays(-1), Now.AddDays(6), Now.AddDays(-1)));
            }
            else if (kind == "B")
            {
                db.ListingPromotions.Add(ListingPromotion.CreateBump(
                    listing.Id, 5m, Payment(i).Id, Now.AddHours(-i)));
            }
        }

        await db.SaveChangesAsync();
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
