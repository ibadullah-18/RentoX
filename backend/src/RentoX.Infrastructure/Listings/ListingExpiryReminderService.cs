using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Listings;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Notifications;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Listings;

public sealed class ListingExpiryReminderService(
    RentoXDbContext dbContext,
    IClock clock)
    : IListingExpiryReminderService
{
    public async Task<int> RemindAsync(
        TimeSpan before,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(before, TimeSpan.FromHours(1));
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);

        DateTimeOffset now = clock.UtcNow;
        DateTimeOffset until = now.Add(before);
        DateTimeOffset askedSince = now.Subtract(before);

        // Listings on show that end soon and have not been reminded about in this stretch.
        // (Renewing moves the end far away, so the next end gets its own reminder.)
        var due = await dbContext.Listings
            .AsNoTracking()
            .Where(listing =>
                listing.Status == ListingStatus.Active &&
                listing.ExpiresAtUtc.HasValue &&
                listing.ExpiresAtUtc.Value > now &&
                listing.ExpiresAtUtc.Value <= until &&
                !dbContext.Set<Notification>().Any(notification =>
                    notification.Type == NotificationType.ListingExpiring &&
                    notification.RelatedEntityId == listing.Id &&
                    notification.CreatedAtUtc > askedSince))
            .OrderBy(listing => listing.ExpiresAtUtc)
            .Select(listing => new { listing.Id, listing.OwnerId, listing.Title, ExpiresAtUtc = listing.ExpiresAtUtc!.Value })
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (due.Count == 0)
        {
            return 0;
        }

        Guid[] owners = due.Select(item => item.OwnerId).Distinct().ToArray();
        HashSet<Guid> withDevice = (await dbContext.Set<PushDevice>()
            .AsNoTracking()
            .Where(device => device.IsActive && owners.Contains(device.UserId))
            .Select(device => device.UserId)
            .Distinct()
            .ToListAsync(cancellationToken)).ToHashSet();

        foreach (var item in due)
        {
            string name = item.Title.Length > 100 ? item.Title[..100] + "…" : item.Title;
            Notification notification = Notification.Create(
                item.OwnerId,
                NotificationType.ListingExpiring,
                "Elanın müddəti bitir",
                $"\"{name}\" elanı {item.ExpiresAtUtc:dd.MM.yyyy} tarixində bitir. Saytda qalması üçün onu yeniləyin.",
                item.Id,
                $"/listings/{item.Id:D}",
                now);
            dbContext.Set<Notification>().Add(notification);

            if (withDevice.Contains(item.OwnerId))
            {
                dbContext.Set<PushNotificationDelivery>().Add(
                    PushNotificationDelivery.Create(notification.Id, notification.UserId, now));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return due.Count;
    }
}
