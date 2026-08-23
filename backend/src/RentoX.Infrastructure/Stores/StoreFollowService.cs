using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Common;
using RentoX.Application.Stores;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Stores;
using RentoX.Domain.Stores.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Stores;

public sealed class StoreFollowService(
    RentoXDbContext dbContext,
    IClock clock)
    : IStoreFollowService
{
    private const int MaximumPageSize = 50;

    public async Task FollowAsync(
        Guid userId,
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        StoreOwnerProjection? store =
            await dbContext.Set<StoreProfile>()
                .AsNoTracking()
                .Where(item =>
                    item.Id == storeId &&
                    item.Status == StoreStatus.Active)
                .Select(item =>
                    new StoreOwnerProjection(
                        item.Id,
                        item.OwnerId))
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (store is null)
        {
            throw new DomainException(
                "Store was not found.");
        }

        if (store.OwnerId == userId)
        {
            throw new DomainException(
                "You cannot follow your own store.");
        }

        Guid followId = Guid.NewGuid();
        DateTimeOffset now = clock.UtcNow;

        await dbContext.Database
            .ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO engagement.store_followers
                    ("Id", "UserId", "StoreId", "CreatedAtUtc")
                VALUES
                    ({followId}, {userId}, {storeId}, {now})
                ON CONFLICT ("UserId", "StoreId")
                DO NOTHING
                """,
                cancellationToken);
    }

    public async Task UnfollowAsync(
        Guid userId,
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        await dbContext.StoreFollowers
            .Where(follower =>
                follower.UserId == userId &&
                follower.StoreId == storeId)
            .ExecuteDeleteAsync(
                cancellationToken);
    }

    public async Task<StoreFollowStatusResult?>
        GetStatusAsync(
            Guid? userId,
            Guid storeId,
            CancellationToken cancellationToken = default)
    {
        bool storeExists =
            await dbContext.Set<StoreProfile>()
                .AsNoTracking()
                .AnyAsync(
                    store =>
                        store.Id == storeId &&
                        store.Status ==
                        StoreStatus.Active,
                    cancellationToken);

        if (!storeExists)
        {
            return null;
        }

        int followerCount =
            await dbContext.StoreFollowers
                .AsNoTracking()
                .CountAsync(
                    follower =>
                        follower.StoreId == storeId,
                    cancellationToken);

        bool isFollowing =
            userId.HasValue &&
            await dbContext.StoreFollowers
                .AsNoTracking()
                .AnyAsync(
                    follower =>
                        follower.UserId ==
                            userId.Value &&
                        follower.StoreId == storeId,
                    cancellationToken);

        return new StoreFollowStatusResult(
            storeId,
            followerCount,
            isFollowing);
    }

    public async Task<PagedResult<FollowedStoreResult>>
        GetFollowingAsync(
            Guid userId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        ValidatePagination(page, pageSize);

        IQueryable<StoreFollower> query =
            dbContext.StoreFollowers
                .AsNoTracking()
                .Where(follower =>
                    follower.UserId == userId);

        int totalCount =
            await query.CountAsync(
                cancellationToken);

        List<FollowedStoreProjection> stores =
            await query
                .Join(
                    dbContext.Set<StoreProfile>()
                        .AsNoTracking()
                        .Where(store =>
                            store.Status ==
                            StoreStatus.Active),
                    follower => follower.StoreId,
                    store => store.Id,
                    (follower, store) =>
                        new FollowedStoreProjection(
                            store.Id,
                            store.Name,
                            store.Slug,
                            store.LogoImageKey,
                            store.CreatedAtUtc,
                            store.UpdatedAtUtc,
                            follower.CreatedAtUtc))
                .OrderByDescending(store =>
                    store.FollowedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

        Guid[] storeIds =
            stores
                .Select(store => store.StoreId)
                .ToArray();

        Dictionary<Guid, int> listingCounts = [];
        Dictionary<Guid, int> followerCounts = [];

        if (storeIds.Length > 0)
        {
            DateTimeOffset now = clock.UtcNow;

            listingCounts =
                await dbContext.Listings
                    .AsNoTracking()
                    .Where(listing =>
                        storeIds.Contains(
                            listing.OwnerId) &&
                        listing.Status ==
                            ListingStatus.Active &&
                        listing.ExpiresAtUtc.HasValue &&
                        listing.ExpiresAtUtc.Value > now)
                    .GroupBy(listing =>
                        listing.OwnerId)
                    .Select(group => new
                    {
                        OwnerId = group.Key,
                        Count = group.Count()
                    })
                    .ToDictionaryAsync(
                        item => item.OwnerId,
                        item => item.Count,
                        cancellationToken);

            followerCounts =
                await dbContext.StoreFollowers
                    .AsNoTracking()
                    .Where(follower =>
                        storeIds.Contains(
                            follower.StoreId))
                    .GroupBy(follower =>
                        follower.StoreId)
                    .Select(group => new
                    {
                        StoreId = group.Key,
                        Count = group.Count()
                    })
                    .ToDictionaryAsync(
                        item => item.StoreId,
                        item => item.Count,
                        cancellationToken);
        }

        List<FollowedStoreResult> items =
            stores
                .Select(store =>
                {
                    DateTimeOffset imageVersion =
                        store.UpdatedAtUtc ??
                        store.CreatedAtUtc;

                    return new FollowedStoreResult(
                        store.StoreId,
                        store.Name,
                        store.Slug,
                        !string.IsNullOrWhiteSpace(
                            store.LogoImageKey),
                        imageVersion
                            .ToUnixTimeMilliseconds(),
                        listingCounts.GetValueOrDefault(
                            store.StoreId),
                        followerCounts.GetValueOrDefault(
                            store.StoreId),
                        store.FollowedAtUtc);
                })
                .ToList();

        int totalPages =
            totalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    totalCount / (double)pageSize);

        return new PagedResult<FollowedStoreResult>(
            items,
            page,
            pageSize,
            totalCount,
            totalPages);
    }

    private static void ValidatePagination(
        int page,
        int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(
            page,
            1);

        ArgumentOutOfRangeException.ThrowIfLessThan(
            pageSize,
            1);

        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            pageSize,
            MaximumPageSize);
    }

    private sealed record StoreOwnerProjection(
        Guid StoreId,
        Guid OwnerId);

    private sealed record FollowedStoreProjection(
        Guid StoreId,
        string Name,
        string Slug,
        string? LogoImageKey,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? UpdatedAtUtc,
        DateTimeOffset FollowedAtUtc);
}