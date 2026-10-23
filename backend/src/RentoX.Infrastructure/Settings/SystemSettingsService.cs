using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Settings;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Settings;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Settings;

public sealed class SystemSettingsService(
    RentoXDbContext dbContext,
    IMemoryCache cache,
    IClock clock,
    ICurrentUserContext currentUserContext)
    : ISystemSettingsService
{
    private const string CacheKey = "settings:current";

    private static readonly TimeSpan Keep = TimeSpan.FromSeconds(30);

    /// <summary>The values the site started with, in force until the first change.</summary>
    public static readonly SystemSettingsValues Starting = new(30, 15, 0, 0);

    public async Task<SystemSettingsValues> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKey, out SystemSettingsValues? known) && known is not null)
        {
            return known;
        }

        SystemSettingsValues current = await dbContext.Set<SettingsVersion>()
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAtUtc)
            .Select(s => new SystemSettingsValues(s.ListingLifetimeDays, s.MaxListingPhotos, s.MinListingPhotos, s.MaxNewListingsPerDay))
            .FirstOrDefaultAsync(cancellationToken)
            ?? Starting;

        cache.Set(CacheKey, current, Keep);
        return current;
    }

    public async Task<SystemSettingsValues> SetAsync(
        SystemSettingsValues values,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);

        Guid? actor = currentUserContext.UserId;
        if (!currentUserContext.IsAuthenticated || !actor.HasValue || actor.Value == Guid.Empty)
        {
            throw new DomainException("An authenticated admin is required.");
        }

        SettingsVersion version = SettingsVersion.Create(
            values.ListingLifetimeDays, values.MaxListingPhotos, values.MinListingPhotos, values.MaxNewListingsPerDay,
            actor.Value, clock.UtcNow);
        dbContext.Set<SettingsVersion>().Add(version);
        await dbContext.SaveChangesAsync(cancellationToken);

        cache.Remove(CacheKey);
        return values;
    }

    public async Task<IReadOnlyList<SystemSettingsHistoryItem>> GetHistoryAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 100);

        var rows = await dbContext.Set<SettingsVersion>()
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAtUtc)
            .Take(take)
            .Select(s => new { s.Id, s.ListingLifetimeDays, s.MaxListingPhotos, s.MinListingPhotos, s.MaxNewListingsPerDay, s.CreatedByUserId, s.CreatedAtUtc })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new SystemSettingsHistoryItem(
                r.Id,
                new SystemSettingsValues(r.ListingLifetimeDays, r.MaxListingPhotos, r.MinListingPhotos, r.MaxNewListingsPerDay),
                r.CreatedByUserId,
                r.CreatedAtUtc))
            .ToArray();
    }
}
