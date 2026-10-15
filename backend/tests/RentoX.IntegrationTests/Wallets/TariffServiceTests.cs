using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Listings.Billing;
using RentoX.Application.Listings.Promotions;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings.Promotions.Enums;
using RentoX.Infrastructure.Listings.Billing;
using RentoX.Infrastructure.Listings.Promotions;
using RentoX.Infrastructure.Persistence;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Wallets;

/// <summary>The prices an admin can change: the starting ones, a change, history, and what uses them.</summary>
public sealed class TariffServiceTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task TheStartingPricesApplyUntilTheFirstChangeThenTheNewOnesDo()
    {
        await ResetAsync();
        MemoryCache cache = new(new MemoryCacheOptions());
        Guid admin = Guid.NewGuid();

        TariffValues first = await Service(admin, cache).GetCurrentAsync();
        Assert.Equal(TariffService.Starting, first);

        TariffValues changed = new(3, 2m, 6m, 15m, 14);
        await Service(admin, cache).SetAsync(changed);

        // The same cache as before: a change shows at once, not after the cache runs out.
        Assert.Equal(changed, await Service(admin, cache).GetCurrentAsync());
    }

    [Fact]
    public async Task EveryChangeIsKeptWithWhoMadeIt()
    {
        await ResetAsync();
        Guid admin = Guid.NewGuid();
        MemoryCache cache = new(new MemoryCacheOptions());

        await Service(admin, cache, Now).SetAsync(new TariffValues(4, 1m, 5m, 10m, 7));
        await Service(admin, cache, Now.AddMinutes(1)).SetAsync(new TariffValues(4, 1.5m, 5m, 10m, 7));

        var history = await Service(admin, cache).GetHistoryAsync(10);

        Assert.True(history.Count >= 2);
        Assert.Equal(1.5m, history[0].Values.ActivationFee);
        Assert.Equal(admin, history[0].ChangedByUserId);
        Assert.True(history[0].ChangedAtUtc > history[1].ChangedAtUtc);
    }

    [Fact]
    public async Task AnInvalidPriceChangesNothing()
    {
        await ResetAsync();
        MemoryCache cache = new(new MemoryCacheOptions());
        TariffValues before = await Service(Guid.NewGuid(), cache).GetCurrentAsync();

        await Assert.ThrowsAsync<DomainException>(() =>
            Service(Guid.NewGuid(), cache).SetAsync(before with { VipPrice = -1m }));

        Assert.Equal(before, await Service(Guid.NewGuid(), new MemoryCache(new MemoryCacheOptions())).GetCurrentAsync());
    }

    [Fact]
    public async Task PromotionsAreChargedAtTheCurrentPrices()
    {
        await ResetAsync();
        MemoryCache cache = new(new MemoryCacheOptions());
        TariffService tariffs = Service(Guid.NewGuid(), cache);
        await tariffs.SetAsync(new TariffValues(5, 1m, 7m, 25m, 30));

        ListingPromotionPricingService pricing = new(tariffs);

        ListingPromotionPricingResult bump = await pricing.GetPricingAsync(ListingPromotionType.Bump);
        ListingPromotionPricingResult vip = await pricing.GetPricingAsync(ListingPromotionType.Vip);

        Assert.Equal(7m, bump.Price);
        Assert.Equal(25m, vip.Price);
        Assert.Equal(30, vip.DurationDays);
    }

    /// <summary>The tests share one database: each starts with no price changes at all.</summary>
    private async Task ResetAsync()
    {
        await using RentoXDbContext db = database.CreateContext();
        await db.Database.ExecuteSqlRawAsync("DELETE FROM payments.tariffs");
    }

    private TariffService Service(Guid actor, IMemoryCache cache, DateTimeOffset? now = null)
    {
        RentoXDbContext db = database.CreateContext();
        return new TariffService(db, cache, new TestClock(now ?? Now), new TestActor(actor));
    }

    private sealed class TestClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class TestActor(Guid userId) : ICurrentUserContext
    {
        public Guid? UserId => userId;

        public bool IsAuthenticated => true;
    }
}
