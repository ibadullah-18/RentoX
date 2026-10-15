using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Settings;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Settings;
using RentoX.Infrastructure.Persistence;
using RentoX.Infrastructure.Settings;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Moderation;

/// <summary>The adjustable settings: starting values, a change, its history and the limits.</summary>
public sealed class SystemSettingsTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly Guid Admin = Guid.NewGuid();

    private static readonly int[] ExpectedHistory = [14, 45];

    [Fact]
    public async Task StartsAtThirtyDaysThenFollowsTheNewestChangeAndKeepsTheHistory()
    {
        await ResetAsync();
        var cache = new MemoryCache(new MemoryCacheOptions());

        Assert.Equal(30, (await Service(cache).GetCurrentAsync()).ListingLifetimeDays);

        await Service(cache).SetAsync(new SystemSettingsValues(45));
        Assert.Equal(45, (await Service(cache).GetCurrentAsync()).ListingLifetimeDays);

        await Service(cache).SetAsync(new SystemSettingsValues(14));
        Assert.Equal(14, (await Service(cache).GetCurrentAsync()).ListingLifetimeDays);

        var history = await Service(cache).GetHistoryAsync(10);
        Assert.Equal(ExpectedHistory, history.Select(h => h.Values.ListingLifetimeDays).ToArray());
        Assert.All(history, h => Assert.Equal(Admin, h.ChangedByUserId));
    }

    [Theory]
    [InlineData(6)]
    [InlineData(366)]
    [InlineData(0)]
    public async Task ALifetimeOutsideTheLimitsIsRefusedAndChangesNothing(int days)
    {
        await ResetAsync();
        var cache = new MemoryCache(new MemoryCacheOptions());

        await Assert.ThrowsAsync<DomainException>(() => Service(cache).SetAsync(new SystemSettingsValues(days)));

        Assert.Equal(30, (await Service(cache).GetCurrentAsync()).ListingLifetimeDays);
        await using RentoXDbContext db = database.CreateContext();
        Assert.False(await db.Set<SettingsVersion>().AnyAsync());
    }

    [Fact]
    public async Task OnlyASignedInAdminCanChangeThem()
    {
        await ResetAsync();
        await using RentoXDbContext db = database.CreateContext();
        var anonymous = new SystemSettingsService(db, new MemoryCache(new MemoryCacheOptions()), new TestClock(), new TestActor(null));

        await Assert.ThrowsAsync<DomainException>(() => anonymous.SetAsync(new SystemSettingsValues(20)));
    }

    private async Task ResetAsync()
    {
        await using RentoXDbContext db = database.CreateContext();
        await db.Set<SettingsVersion>().ExecuteDeleteAsync();
    }

    private SystemSettingsService Service(IMemoryCache cache) =>
        new(database.CreateContext(), cache, new TestClock(), new TestActor(Admin));

    private sealed class TestClock : IClock
    {
        private static int ticks;

        // Each call is a little later, so "the newest" is well defined.
        public DateTimeOffset UtcNow => new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero).AddSeconds(Interlocked.Increment(ref ticks));
    }

    private sealed class TestActor(Guid? userId) : ICurrentUserContext
    {
        public Guid? UserId => userId;

        public bool IsAuthenticated => userId is not null;
    }
}
