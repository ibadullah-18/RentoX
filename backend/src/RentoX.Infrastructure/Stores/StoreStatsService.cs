using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Stores;
using RentoX.Domain.Hr;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Stores;
using RentoX.Domain.Stores.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Stores;

public sealed class StoreStatsService(RentoXDbContext dbContext, IClock clock) : IStoreStatsService
{
    public async Task<bool> RecordAsync(
        string slug, string kind, Guid? viewerUserId, CancellationToken cancellationToken = default)
    {
        string normalized = (slug ?? string.Empty).Trim().ToLowerInvariant();
        string what = (kind ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized.Length is 0 or > 120 || !StoreDayStat.Kinds.Contains(what))
        {
            return false;
        }

        var store = await dbContext.Set<StoreProfile>()
            .AsNoTracking()
            .Where(s => s.Slug == normalized && s.Status == StoreStatus.Active)
            .Select(s => new { s.Id, s.OwnerId })
            .SingleOrDefaultAsync(cancellationToken);

        // A store that is not live, and the owner looking at their own store, are not counted.
        if (store is null || viewerUserId == store.OwnerId)
        {
            return false;
        }

        DateOnly day = HrTime.BakuDay(clock.UtcNow);
        Guid id = store.Id;

        // One statement adds one: the row of the day is made on the first event and counted on the rest.
        _ = what switch
        {
            "view" => await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""INSERT INTO stores.store_day_stats ("StoreId","Day","Views","Calls","WhatsApp","Messages") VALUES ({id},{day},1,0,0,0) ON CONFLICT ("StoreId","Day") DO UPDATE SET "Views" = stores.store_day_stats."Views" + 1""",
                cancellationToken),
            "call" => await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""INSERT INTO stores.store_day_stats ("StoreId","Day","Views","Calls","WhatsApp","Messages") VALUES ({id},{day},0,1,0,0) ON CONFLICT ("StoreId","Day") DO UPDATE SET "Calls" = stores.store_day_stats."Calls" + 1""",
                cancellationToken),
            "whatsapp" => await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""INSERT INTO stores.store_day_stats ("StoreId","Day","Views","Calls","WhatsApp","Messages") VALUES ({id},{day},0,0,1,0) ON CONFLICT ("StoreId","Day") DO UPDATE SET "WhatsApp" = stores.store_day_stats."WhatsApp" + 1""",
                cancellationToken),
            _ => await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""INSERT INTO stores.store_day_stats ("StoreId","Day","Views","Calls","WhatsApp","Messages") VALUES ({id},{day},0,0,0,1) ON CONFLICT ("StoreId","Day") DO UPDATE SET "Messages" = stores.store_day_stats."Messages" + 1""",
                cancellationToken)
        };

        return true;
    }

    public async Task<StoreStatsResult?> GetMineAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        var store = await dbContext.Set<StoreProfile>()
            .AsNoTracking()
            .Where(s => s.OwnerId == ownerId)
            .Select(s => new { s.Id })
            .SingleOrDefaultAsync(cancellationToken);
        if (store is null)
        {
            return null;
        }

        DateTimeOffset now = clock.UtcNow;
        DateOnly today = HrTime.BakuDay(now);
        DateOnly from30 = today.AddDays(-29);
        DateOnly from7 = today.AddDays(-6);

        long followers = await dbContext.StoreFollowers.AsNoTracking().LongCountAsync(f => f.StoreId == store.Id, cancellationToken);

        var live = dbContext.Listings
            .AsNoTracking()
            .Where(l => l.OwnerId == ownerId && l.Status == ListingStatus.Active && l.ExpiresAtUtc.HasValue && l.ExpiresAtUtc.Value > now);
        long listings = await live.LongCountAsync(cancellationToken);
        long listingViews = await live.Select(l => (long?)l.ViewCount).SumAsync(cancellationToken) ?? 0;
        var liveIds = live.Select(l => l.Id);
        long favorites = await dbContext.Favorites.AsNoTracking().LongCountAsync(f => liveIds.Contains(f.ListingId), cancellationToken);

        List<StoreDayStat> rows = await dbContext.Set<StoreDayStat>()
            .AsNoTracking()
            .Where(s => s.StoreId == store.Id)
            .ToListAsync(cancellationToken);

        StoreDayPoint[] series = Enumerable.Range(0, 30)
            .Select(i => from30.AddDays(i))
            .Select(d =>
            {
                StoreDayStat? r = rows.FirstOrDefault(x => x.Day == d);
                return new StoreDayPoint(d, r?.Views ?? 0, r?.Calls ?? 0, r?.WhatsApp ?? 0, r?.Messages ?? 0);
            })
            .ToArray();

        return new StoreStatsResult(
            followers,
            listings,
            listingViews,
            favorites,
            rows.Sum(r => (long)r.Views),
            rows.Sum(r => (long)r.Calls),
            rows.Sum(r => (long)r.WhatsApp),
            rows.Sum(r => (long)r.Messages),
            series.Where(p => p.Day >= from7).Sum(p => (long)p.Views),
            series.Sum(p => (long)p.Views),
            series.Sum(p => (long)p.Calls + p.WhatsApp + p.Messages),
            series);
    }
}
