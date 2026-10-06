using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Notifications;

namespace RentoX.Domain.UnitTests.Notifications;

public sealed class NotificationTests
{
    [Fact]
    public void CreateNormalizesContentAndStartsUnread()
    {
        Notification notification = Notification.Create(
            Guid.NewGuid(),
            NotificationType.ListingApproved,
            "  Elan təsdiqləndi  ",
            "  Elan artıq aktivdir.  ",
            Guid.NewGuid(),
            "  /listings/123  ",
            DateTimeOffset.UtcNow);

        Assert.Equal("Elan təsdiqləndi", notification.Title);
        Assert.Equal("Elan artıq aktivdir.", notification.Body);
        Assert.Equal("/listings/123", notification.ActionUrl);
        Assert.Null(notification.ReadAtUtc);
    }

    [Fact]
    public void CreateRejectsEmptyUserId()
    {
        Assert.Throws<DomainException>(() => Notification.Create(
            Guid.Empty,
            NotificationType.ListingApproved,
            "Title",
            "Body",
            null,
            null,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void MarkAsReadIsIdempotent()
    {
        Notification notification = Notification.Create(
            Guid.NewGuid(),
            NotificationType.StoreApproved,
            "Store approved",
            "Store is active.",
            null,
            null,
            DateTimeOffset.UtcNow);
        DateTimeOffset firstReadAt = DateTimeOffset.UtcNow;

        notification.MarkAsRead(firstReadAt);
        notification.MarkAsRead(firstReadAt.AddMinutes(1));

        Assert.Equal(firstReadAt, notification.ReadAtUtc);
    }

    [Fact]
    public void CreateRejectsNonUtcTimestamp()
    {
        Assert.Throws<DomainException>(() => Notification.Create(
            Guid.NewGuid(),
            NotificationType.StoreRejected,
            "Store rejected",
            "Reason",
            null,
            null,
            new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.FromHours(4))));
    }
}
