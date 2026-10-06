using RentoX.Application.Abstractions.Time;
using System.Data;
using RentoX.Domain.Auditing;
using RentoX.Application.Abstractions.Authentication;
using Microsoft.EntityFrameworkCore;
using RentoX.Application.Common;
using RentoX.Application.Notifications;
using RentoX.Domain.Notifications;
using RentoX.Application.Stores;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Stores;
using RentoX.Domain.Stores.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Stores;

public sealed class StoreModerationService(
    RentoXDbContext dbContext,
    INotificationService notificationService,
    ICurrentUserContext currentUserContext,
    IClock clock)
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
        Guid actorUserId = RequireAuditActor();
        Guid operationId = Guid.NewGuid();

        await using var auditTransaction =
            await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        StoreProfile store =
            await GetRequiredStoreAsync(
                storeId,
                cancellationToken);

        StoreStatus previousStatus = store.Status;

        store.Approve();

        RecordStoreStatusChange(
            store, previousStatus, actorUserId, operationId);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await auditTransaction.CommitAsync(cancellationToken);

        await notificationService.CreateAsync(
            new CreateNotificationCommand(
                store.OwnerId,
                (int)NotificationType.StoreApproved,
                "Mağaza təsdiqləndi",
                $"\"{store.Name}\" mağazası təsdiqləndi və aktivləşdirildi.",
                store.Id,
                $"/stores/{store.Slug}"),
            cancellationToken);

        return MapStatus(store);
    }

    public async Task<StoreStatusResult> RejectAsync(
        Guid storeId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        Guid actorUserId = RequireAuditActor();
        Guid operationId = Guid.NewGuid();

        await using var auditTransaction =
            await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        StoreProfile store =
            await GetRequiredStoreAsync(
                storeId,
                cancellationToken);

        StoreStatus previousStatus = store.Status;

        store.Reject(reason);

        RecordStoreStatusChange(
            store, previousStatus, actorUserId, operationId);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await auditTransaction.CommitAsync(cancellationToken);

        await notificationService.CreateAsync(
            new CreateNotificationCommand(
                store.OwnerId,
                (int)NotificationType.StoreRejected,
                "Mağaza rədd edildi",
                $"\"{store.Name}\" mağazası rədd edildi. Səbəb: {reason.Trim()}",
                store.Id,
                $"/profile/store"),
            cancellationToken);

        return MapStatus(store);
    }

    private Guid RequireAuditActor()
    {
        Guid? userId = currentUserContext.UserId;

        if (!currentUserContext.IsAuthenticated ||
            !userId.HasValue ||
            userId.Value == Guid.Empty)
        {
            throw new DomainException(
                "An authenticated audit actor is required.");
        }

        return userId.Value;
    }

    private void RecordStoreStatusChange(
        StoreProfile store,
        StoreStatus previousStatus,
        Guid actorUserId,
        Guid operationId)
    {
        if (previousStatus == store.Status)
        {
            return;
        }

        dbContext.Set<AuditLogEntry>().Add(
            AuditLogEntry.ForStoreStatusChange(
                operationId,
                actorUserId,
                store.Id,
                previousStatus,
                store.Status,
                clock.UtcNow));
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