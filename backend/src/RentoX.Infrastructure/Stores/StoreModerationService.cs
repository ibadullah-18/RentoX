using Microsoft.EntityFrameworkCore;
using RentoX.Application.Common;
using RentoX.Application.Stores;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Stores;
using RentoX.Domain.Stores.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Stores;

public sealed class StoreModerationService(
    RentoXDbContext dbContext)
    : IStoreModerationService
{
    private const int MaximumPageSize = 50;

    public async Task<PagedResult<StoreProfileResult>>
        GetPendingAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        ValidatePagination(
            page,
            pageSize);

        IQueryable<StoreProfile> query =
            dbContext.StoreProfiles
                .AsNoTracking()
                .Where(store =>
                    store.Status ==
                        StoreStatus.PendingReview);

        int totalCount =
            await query.CountAsync(
                cancellationToken);

        List<StoreProfile> stores =
            await query
                .OrderBy(store =>
                    store.UpdatedAtUtc ??
                    store.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

        List<StoreProfileResult> items =
            stores
                .Select(MapProfile)
                .ToList();

        int totalPages =
            totalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    totalCount /
                    (double)pageSize);

        return new PagedResult<StoreProfileResult>(
            items,
            page,
            pageSize,
            totalCount,
            totalPages);
    }

    public async Task<StoreProfileResult?> GetByIdAsync(
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        StoreProfile? store =
            await dbContext.StoreProfiles
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.Id == storeId,
                    cancellationToken);

        return store is null
            ? null
            : MapProfile(store);
    }

    public async Task<StoreStatusResult> ApproveAsync(
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        StoreProfile store =
            await GetRequiredStoreAsync(
                storeId,
                cancellationToken);

        store.Approve();

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return MapStatus(store);
    }

    public async Task<StoreStatusResult> RejectAsync(
        Guid storeId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        StoreProfile store =
            await GetRequiredStoreAsync(
                storeId,
                cancellationToken);

        store.Reject(reason);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return MapStatus(store);
    }

    private async Task<StoreProfile> GetRequiredStoreAsync(
        Guid storeId,
        CancellationToken cancellationToken)
    {
        return await dbContext.StoreProfiles
            .SingleOrDefaultAsync(
                store => store.Id == storeId,
                cancellationToken)
            ?? throw new DomainException(
                "Store was not found.");
    }

    private static StoreProfileResult MapProfile(
        StoreProfile store)
    {
        return new StoreProfileResult(
            store.Id,
            store.OwnerId,
            store.Name,
            store.Slug,
            store.Description,
            store.PhoneNumber,
            store.Email,
            store.Address,
            store.LogoImageKey,
            store.CoverImageKey,
            store.InstagramUrl,
            store.TiktokUrl,
            store.FacebookUrl,
            store.WebsiteUrl,
            (int)store.Status,
            store.RejectionReason,
            store.CreatedAtUtc,
            store.UpdatedAtUtc);
    }

    private static StoreStatusResult MapStatus(
        StoreProfile store)
    {
        return new StoreStatusResult(
            store.Id,
            (int)store.Status,
            store.RejectionReason,
            store.UpdatedAtUtc);
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
}