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

    private static string Pattern(ListingFeedInterleaver.Plan plan) =>
        string.Concat(plan.Page.Select(k => "VBP"[k]));

    [Fact]
    public void RoundsAlternateKindsAndTheVipOnesComeRoundAgainWhileOthersRemain()
    {
        ListingFeedInterleaver.Plan plan =
            ListingFeedInterleaver.Build(14, 6, 20, 0, 80);

        // 14 VIP, 6 bumped, 20 plain. The VIP slots keep being filled: after the 14th comes the 1st again.
        Assert.Equal(
            "VVVV" + "BBBB" + "PPPP" +
            "VVVV" + "BB" + "PPPP" +
            "VVVV" + "PPPP" +
            "VVVV" + "PPPP" +
            "VVVV" + "PPPP",
            Pattern(plan));
        Assert.Equal(46, plan.Total);
        Assert.Equal(46, plan.Page.Count);

        int[] vip = plan.Page
            .Select((k, i) => (k, i: plan.Indices[i]))
            .Where(x => x.k == ListingFeedInterleaver.Vip)
            .Select(x => x.i)
            .ToArray();
        Assert.Equal([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 0, 1, 2, 3, 4, 5], vip);
    }

    [Fact]
    public void TheWideFeedShowsTwoRowsOfVipThreeMovedUpThreeOrdinaryOfFive()
    {
        ListingFeedInterleaver.Plan plan =
            ListingFeedInterleaver.Build(30, 30, 30, 0, 80, ListingFeedInterleaver.WideRound);

        Assert.Equal(
            new string('V', 10) + new string('B', 15) + new string('P', 15) + new string('V', 10) + new string('B', 15) + new string('P', 15),
            Pattern(plan));
    }

    [Fact]
    public void SkipCountsWhatComesBeforeThePage()
    {
        // Page 2 of size 8 starts at position 8: after 4 VIP, 4 bumped and the first 4 plain ones.
        ListingFeedInterleaver.Plan plan =
            ListingFeedInterleaver.Build(14, 6, 20, 8, 8);

        Assert.Equal("PPPPVVVV", Pattern(plan));
        Assert.Equal([0, 1, 2, 3, 4, 5, 6, 7], plan.Indices);
    }

    [Fact]
    public void ANoVipFeedIsJustBumpedThenPlain()
    {
        ListingFeedInterleaver.Plan plan =
            ListingFeedInterleaver.Build(0, 3, 5, 0, 20);

        Assert.Equal("BBBPPPPP", Pattern(plan));
        Assert.Equal(8, plan.Total);
    }

    [Fact]
    public void WithFewVipListingsTheOnesThereAreComeRoundButNeverTwiceInOneRound()
    {
        // One VIP listing: it comes once in every round, not four times in a row.
        ListingFeedInterleaver.Plan one = ListingFeedInterleaver.Build(1, 0, 8, 0, 30);
        Assert.Equal("VPPPP" + "VPPPP", Pattern(one));
        Assert.All(one.Page.Select((k, i) => (k, i: one.Indices[i])).Where(x => x.k == 0), x => Assert.Equal(0, x.i));

        // Six VIP listings, ordinary ones for three rounds: 4, then 2 and the first 2 again, then 4 more.
        ListingFeedInterleaver.Plan six = ListingFeedInterleaver.Build(6, 0, 12, 0, 40);
        Assert.Equal("VVVVPPPP" + "VVVVPPPP" + "VVVVPPPP", Pattern(six));
        Assert.Equal(
            [0, 1, 2, 3, 4, 5, 0, 1, 2, 3, 4, 5],
            six.Page.Select((k, i) => (k, i: six.Indices[i])).Where(x => x.k == 0).Select(x => x.i));
    }

    [Fact]
    public void WhenNothingElseIsLeftEachVipListingIsShownOnceAndTheFeedEnds()
    {
        ListingFeedInterleaver.Plan onlyVip = ListingFeedInterleaver.Build(6, 0, 0, 0, 30);
        Assert.Equal("VVVVVV", Pattern(onlyVip));
        Assert.Equal([0, 1, 2, 3, 4, 5], onlyVip.Indices);
        Assert.Equal(6, onlyVip.Total);

        Assert.Equal(0, ListingFeedInterleaver.Build(0, 0, 0, 0, 30).Total);
    }

    [Fact]
    public async Task TheFeedMixesVipBumpedAndPlainListings()
    {
        await EnsureSeededAsync();

        List<string> first = await FeedAsync(1, 20);
        string pattern = string.Concat(first.Select(t => t[0]));

        // 4 VIP, 4 bumped, 4 plain, then VIP again.
        Assert.Equal("VVVV" + "BBBB" + "PPPP" + "VVVV" + "BB" + "PP", pattern);

        // Free listings are reachable on the very first page even though
        // there are more VIP listings than a page can hold.
        Assert.Contains(first, t => t.StartsWith('P'));
    }

    [Fact]
    public async Task PagingNeverLosesAListingAndTheVipOnesComeRoundAgain()
    {
        await EnsureSeededAsync();

        List<string> all = [];

        for (int page = 1; page <= 5; page++)
        {
            all.AddRange(await FeedAsync(page, 10));
        }

        // 40 listings, but 14 VIP ones come round again: 6 more places.
        Assert.Equal(46, all.Count);
        Assert.Equal(40, all.Distinct().Count());

        // Only VIP listings are repeated, and the first one is the first to come back.
        List<string> repeated = [.. all.GroupBy(t => t).Where(g => g.Count() > 1).Select(g => g.Key)];
        Assert.All(repeated, t => Assert.StartsWith("V", t, StringComparison.Ordinal));
        Assert.Equal(6, repeated.Count);

        // Same order whatever the page size.
        Assert.Equal(all, await FeedAsync(1, 50));
        Assert.Equal(all, (await FeedAsync(1, 7)).Concat(await FeedAsync(2, 7)).Concat(await FeedAsync(3, 7))
            .Concat(await FeedAsync(4, 7)).Concat(await FeedAsync(5, 7)).Concat(await FeedAsync(6, 7))
            .Concat(await FeedAsync(7, 7)));
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

        Assert.Equal(46, result.TotalCount);

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
