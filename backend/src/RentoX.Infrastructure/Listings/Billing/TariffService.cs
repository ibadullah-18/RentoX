using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Listings.Billing;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings.Billing;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Listings.Billing;

public sealed class TariffService(
    RentoXDbContext dbContext,
    IMemoryCache cache,
    IClock clock,
    ICurrentUserContext currentUserContext)
    : ITariffService
{
    private const string CacheKey = "tariff:current";

    private static readonly TimeSpan Keep = TimeSpan.FromSeconds(30);

    /// <summary>The prices the site started with, in force until the first change.</summary>
    public static readonly TariffValues Starting = new(5, 1m, 5m, 10m, 7);

    public async Task<TariffValues> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKey, out TariffValues? known) && known is not null)
        {
            return known;
        }

        TariffValues current = await dbContext.Set<TariffVersion>()
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new TariffValues(
                t.FreeActiveListingLimit, t.ActivationFee, t.BumpPrice, t.VipPrice, t.VipDurationDays))
            .FirstOrDefaultAsync(cancellationToken)
            ?? Starting;

        cache.Set(CacheKey, current, Keep);
        return current;
    }

    public async Task<TariffValues> SetAsync(TariffValues values, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);

        Guid? actor = currentUserContext.UserId;
        if (!currentUserContext.IsAuthenticated || !actor.HasValue || actor.Value == Guid.Empty)
        {
            throw new DomainException("An authenticated admin is required.");
        }

        TariffVersion version = TariffVersion.Create(
            values.FreeActiveListingLimit,
            values.ActivationFee,
            values.BumpPrice,
            values.VipPrice,
            values.VipDurationDays,
            actor.Value,
            clock.UtcNow);

        dbContext.Set<TariffVersion>().Add(version);
        await dbContext.SaveChangesAsync(cancellationToken);

        cache.Remove(CacheKey);
        return values;
    }

    public async Task<IReadOnlyList<TariffHistoryItem>> GetHistoryAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 100);

        var rows = await dbContext.Set<TariffVersion>()
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(take)
            .Select(t => new
            {
                t.Id,
                t.FreeActiveListingLimit,
                t.ActivationFee,
                t.BumpPrice,
                t.VipPrice,
                t.VipDurationDays,
                t.CreatedByUserId,
                t.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new TariffHistoryItem(
                r.Id,
                new TariffValues(r.FreeActiveListingLimit, r.ActivationFee, r.BumpPrice, r.VipPrice, r.VipDurationDays),
                r.CreatedByUserId,
                r.CreatedAtUtc))
            .ToArray();
    }
}
