using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Common;
using RentoX.Application.Notifications;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Notifications;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Notifications;

public sealed class NotificationService(
    RentoXDbContext dbContext,
    IClock clock,
    INotificationEventPublisher eventPublisher)
    : INotificationService
{
    public async Task<NotificationResult> CreateAsync(
        CreateNotificationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!Enum.IsDefined(typeof(NotificationType), command.Type))
        {
            throw new DomainException("Notification type is invalid.");
        }

        DateTimeOffset now = clock.UtcNow;
        Notification notification = Notification.Create(
            command.UserId,
            (NotificationType)command.Type,
            command.Title,
            command.Body,
            command.RelatedEntityId,
            command.ActionUrl,
            now);

        bool hasActivePushDevice = await dbContext.Set<PushDevice>()
            .AsNoTracking()
            .AnyAsync(device =>
                device.UserId == command.UserId && device.IsActive,
                cancellationToken);

        dbContext.Set<Notification>().Add(notification);
        if (hasActivePushDevice)
        {
            dbContext.Set<PushNotificationDelivery>().Add(
                PushNotificationDelivery.Create(
                    notification.Id, notification.UserId, now));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        NotificationResult result = Map(notification);
        await eventPublisher.NotificationCreatedAsync(result, cancellationToken);
        return result;
    }

    public async Task<PagedResult<NotificationResult>> GetAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        RequireUser(userId);
        ValidatePagination(page, pageSize);

        IQueryable<Notification> query = dbContext.Set<Notification>()
            .AsNoTracking()
            .Where(notification => notification.UserId == userId);

        int totalCount = await query.CountAsync(cancellationToken);
        NotificationResult[] items = await query
            .OrderByDescending(notification => notification.CreatedAtUtc)
            .ThenByDescending(notification => notification.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(notification => new NotificationResult(
                notification.Id,
                notification.UserId,
                (int)notification.Type,
                notification.Title,
                notification.Body,
                notification.RelatedEntityId,
                notification.ActionUrl,
                notification.CreatedAtUtc,
                notification.ReadAtUtc))
            .ToArrayAsync(cancellationToken);

        int totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResult<NotificationResult>(
            items, page, pageSize, totalCount, totalPages);
    }

    public Task<int> GetUnreadCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        RequireUser(userId);
        return dbContext.Set<Notification>()
            .AsNoTracking()
            .CountAsync(notification =>
                notification.UserId == userId &&
                notification.ReadAtUtc == null,
                cancellationToken);
    }

    public async Task<bool> MarkAsReadAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        RequireUser(userId);
        Notification? notification = await dbContext.Set<Notification>()
            .SingleOrDefaultAsync(item =>
                item.Id == notificationId && item.UserId == userId,
                cancellationToken);
        if (notification is null) { return false; }

        notification.MarkAsRead(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> MarkAllAsReadAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        RequireUser(userId);
        DateTimeOffset now = clock.UtcNow;
        return await dbContext.Set<Notification>()
            .Where(notification =>
                notification.UserId == userId &&
                notification.ReadAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    notification => notification.ReadAtUtc,
                    now),
                cancellationToken);
    }

    private static NotificationResult Map(Notification notification)
    {
        return new NotificationResult(
            notification.Id,
            notification.UserId,
            (int)notification.Type,
            notification.Title,
            notification.Body,
            notification.RelatedEntityId,
            notification.ActionUrl,
            notification.CreatedAtUtc,
            notification.ReadAtUtc);
    }

    private static void RequireUser(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("Notification user id is required.");
        }
    }

    private static void ValidatePagination(int page, int pageSize)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            throw new DomainException(
                "Page must be positive and page size must be between 1 and 100.");
        }
    }
}
