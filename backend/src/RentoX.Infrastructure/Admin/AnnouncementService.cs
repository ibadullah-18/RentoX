using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Admin;
using RentoX.Application.Common;
using RentoX.Domain.Auditing;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Notifications;
using RentoX.Domain.Stores.Enums;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Admin;

public sealed class AnnouncementService(
    RentoXDbContext dbContext,
    IClock clock,
    ICurrentUserContext currentUserContext)
    : IAnnouncementService
{
    private const int BatchSize = 500;

    public async Task<PagedResult<AnnouncementHistoryItem>> GetHistoryAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        // One send writes the same text and time to every recipient, so those make the group.
        var groups = dbContext.Set<Notification>()
            .AsNoTracking()
            .Where(n => n.Type == NotificationType.Announcement)
            .GroupBy(n => new { n.Title, n.Body, n.ActionUrl, n.CreatedAtUtc });

        int total = await groups.CountAsync(cancellationToken);
        AnnouncementHistoryItem[] items = await groups
            .OrderByDescending(g => g.Key.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new AnnouncementHistoryItem(
                g.Key.Title,
                g.Key.Body,
                g.Key.ActionUrl,
                g.Key.CreatedAtUtc,
                g.Count(),
                g.Count(n => n.ReadAtUtc != null)))
            .ToArrayAsync(cancellationToken);

        int pages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
        return new PagedResult<AnnouncementHistoryItem>(items, page, pageSize, total, pages);
    }

    public async Task<AnnouncementResult> SendAsync(
        Guid? userId,
        AnnouncementAudience audience,
        string title,
        string body,
        string? actionUrl,
        CancellationToken cancellationToken = default)
    {
        Guid actor = currentUserContext.UserId is { } id && currentUserContext.IsAuthenticated && id != Guid.Empty
            ? id
            : throw new DomainException("An authenticated audit actor is required.");

        // Checks the text once, before anybody is written to.
        _ = Notification.Create(actor, NotificationType.Announcement, title, body, null, actionUrl, clock.UtcNow);

        IQueryable<UserProfile> people = dbContext.Set<UserProfile>()
            .AsNoTracking()
            .Where(p => p.Status == UserStatus.Active);

        if (userId is not null)
        {
            people = people.Where(p => p.Id == userId);
        }
        else if (audience == AnnouncementAudience.StoreOwners)
        {
            people = people.Where(p => dbContext.StoreProfiles.Any(s =>
                s.OwnerId == p.Id && s.Status == StoreStatus.Active));
        }
        else if (audience == AnnouncementAudience.Sellers)
        {
            people = people.Where(p => dbContext.Listings.Any(l =>
                l.OwnerId == p.Id && l.Status == ListingStatus.Active));
        }
        else if (audience != AnnouncementAudience.Everyone)
        {
            throw new DomainException("Audience is invalid.");
        }

        if (userId is null)
        {
            // A group message respects the choice to stop them; a message to one person is a direct one.
            people = people.Where(p => p.AnnouncementsEnabled);
        }

        Guid[] recipients = await people.Select(p => p.Id).ToArrayAsync(cancellationToken);
        if (recipients.Length == 0)
        {
            throw new DomainException(userId is null
                ? "There is nobody to send the message to."
                : "The user was not found or is not active.");
        }

        HashSet<Guid> withDevice = (await dbContext.Set<PushDevice>()
            .AsNoTracking()
            .Where(d => d.IsActive)
            .Select(d => d.UserId)
            .Distinct()
            .ToListAsync(cancellationToken)).ToHashSet();

        DateTimeOffset now = clock.UtcNow;
        Guid operation = Guid.NewGuid();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        foreach (Guid[] batch in recipients.Chunk(BatchSize))
        {
            foreach (Guid recipient in batch)
            {
                Notification notification = Notification.Create(
                    recipient, NotificationType.Announcement, title, body, null, actionUrl, now);
                dbContext.Set<Notification>().Add(notification);
                if (withDevice.Contains(recipient))
                {
                    dbContext.Set<PushNotificationDelivery>().Add(
                        PushNotificationDelivery.Create(notification.Id, recipient, now));
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
        }

        dbContext.Set<AuditLogEntry>().Add(AuditLogEntry.ForAnnouncement(
            operation, actor, userId ?? operation, recipients.Length, now));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new AnnouncementResult(recipients.Length);
    }
}
