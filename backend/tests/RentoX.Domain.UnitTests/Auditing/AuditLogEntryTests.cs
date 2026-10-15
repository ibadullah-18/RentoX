using RentoX.Domain.Auditing;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Stores.Enums;
using RentoX.Domain.Support;

namespace RentoX.Domain.UnitTests.Auditing;

public sealed class AuditLogEntryTests
{
    private static readonly DateTimeOffset OccurredAtUtc =
        new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ListingChangeShouldPreserveActorAndValues()
    {
        Guid operationId = Guid.NewGuid();
        Guid actorId = Guid.NewGuid();
        Guid listingId = Guid.NewGuid();

        AuditLogEntry entry = AuditLogEntry.ForListingStatusChange(
            operationId,
            actorId,
            listingId,
            ListingStatus.PendingReview,
            ListingStatus.Active,
            OccurredAtUtc);

        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Equal(operationId, entry.OperationId);
        Assert.Equal(actorId, entry.ActorUserId);
        Assert.Equal(listingId, entry.TargetId);
        Assert.Equal(AuditAction.ListingStatusChanged, entry.Action);
        Assert.Equal((int?)ListingStatus.PendingReview, entry.PreviousValue);
        Assert.Equal((int?)ListingStatus.Active, entry.CurrentValue);
        Assert.Equal(OccurredAtUtc, entry.OccurredAtUtc);
        Assert.Null(entry.RelatedEntityId);
    }

    [Fact]
    public void StaffRoleChangeRecordsGrantAndRevoke()
    {
        Guid actor = Guid.NewGuid();
        Guid person = Guid.NewGuid();

        AuditLogEntry granted = AuditLogEntry.ForStaffRoleChange(
            Guid.NewGuid(), actor, person, true, OccurredAtUtc);
        AuditLogEntry revoked = AuditLogEntry.ForStaffRoleChange(
            Guid.NewGuid(), actor, person, false, OccurredAtUtc);

        Assert.Equal(AuditAction.StaffRoleChanged, granted.Action);
        Assert.Equal(person, granted.TargetId);
        Assert.Equal(0, granted.PreviousValue);
        Assert.Equal(1, granted.CurrentValue);
        Assert.Equal(1, revoked.PreviousValue);
        Assert.Equal(0, revoked.CurrentValue);
    }

    [Fact]
    public void UserStatusChangeKeepsTheStatusValues()
    {
        AuditLogEntry entry = AuditLogEntry.ForUserStatusChange(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            RentoX.Domain.Users.Enums.UserStatus.Active,
            RentoX.Domain.Users.Enums.UserStatus.Blocked,
            OccurredAtUtc);

        Assert.Equal(AuditAction.UserStatusChanged, entry.Action);
        Assert.Equal(1, entry.PreviousValue);
        Assert.Equal(3, entry.CurrentValue);
    }

    [Fact]
    public void StoreChangeShouldUseStoreAction()
    {
        AuditLogEntry entry = AuditLogEntry.ForStoreStatusChange(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            StoreStatus.PendingReview,
            StoreStatus.Rejected,
            OccurredAtUtc);

        Assert.Equal(AuditAction.StoreStatusChanged, entry.Action);
        Assert.Equal((int?)StoreStatus.PendingReview, entry.PreviousValue);
        Assert.Equal((int?)StoreStatus.Rejected, entry.CurrentValue);
    }

    [Fact]
    public void SupportStatusChangeShouldPreserveValues()
    {
        AuditLogEntry entry = AuditLogEntry.ForSupportStatusChange(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            SupportTicketStatus.Open,
            SupportTicketStatus.InProgress,
            OccurredAtUtc);

        Assert.Equal(AuditAction.SupportStatusChanged, entry.Action);
        Assert.Equal((int?)SupportTicketStatus.Open, entry.PreviousValue);
        Assert.Equal((int?)SupportTicketStatus.InProgress, entry.CurrentValue);
    }

    [Fact]
    public void PriorityChangeShouldUsePriorityAction()
    {
        AuditLogEntry entry = AuditLogEntry.ForSupportPriorityChange(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            SupportTicketPriority.Normal,
            SupportTicketPriority.High,
            OccurredAtUtc);

        Assert.Equal(AuditAction.SupportPriorityChanged, entry.Action);
        Assert.Equal((int?)SupportTicketPriority.Normal, entry.PreviousValue);
        Assert.Equal((int?)SupportTicketPriority.High, entry.CurrentValue);
    }

    [Fact]
    public void ReplyShouldStoreMessageIdWithoutMessageContent()
    {
        Guid messageId = Guid.NewGuid();

        AuditLogEntry entry = AuditLogEntry.ForSupportReply(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            messageId, OccurredAtUtc);

        Assert.Equal(AuditAction.SupportReplyAdded, entry.Action);
        Assert.Equal((Guid?)messageId, entry.RelatedEntityId);
        Assert.Null(entry.PreviousValue);
        Assert.Null(entry.CurrentValue);
    }

    [Fact]
    public void UnchangedStatusShouldBeRejected()
    {
        Assert.Throws<DomainException>(() =>
            AuditLogEntry.ForListingStatusChange(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                ListingStatus.Active,
                ListingStatus.Active,
                OccurredAtUtc));
    }

    [Fact]
    public void InvalidEnumShouldBeRejected()
    {
        Assert.Throws<DomainException>(() =>
            AuditLogEntry.ForListingStatusChange(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                (ListingStatus)999,
                ListingStatus.Active,
                OccurredAtUtc));
    }

    [Fact]
    public void EmptyActorShouldBeRejected()
    {
        Assert.Throws<DomainException>(() =>
            AuditLogEntry.ForSupportReply(
                Guid.NewGuid(), Guid.Empty, Guid.NewGuid(),
                Guid.NewGuid(), OccurredAtUtc));
    }

    [Fact]
    public void EmptyOperationShouldBeRejected()
    {
        Assert.Throws<DomainException>(() =>
            AuditLogEntry.ForSupportReply(
                Guid.Empty, Guid.NewGuid(), Guid.NewGuid(),
                Guid.NewGuid(), OccurredAtUtc));
    }

    [Fact]
    public void EmptyTargetShouldBeRejected()
    {
        Assert.Throws<DomainException>(() =>
            AuditLogEntry.ForSupportReply(
                Guid.NewGuid(), Guid.NewGuid(), Guid.Empty,
                Guid.NewGuid(), OccurredAtUtc));
    }

    [Fact]
    public void EmptyMessageIdShouldBeRejected()
    {
        Assert.Throws<DomainException>(() =>
            AuditLogEntry.ForSupportReply(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                Guid.Empty, OccurredAtUtc));
    }

    [Fact]
    public void NonUtcTimeShouldBeRejected()
    {
        Assert.Throws<DomainException>(() =>
            AuditLogEntry.ForSupportReply(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                Guid.NewGuid(),
                OccurredAtUtc.ToOffset(TimeSpan.FromHours(4))));
    }

    [Fact]
    public void DefaultTimeShouldBeRejected()
    {
        Assert.Throws<DomainException>(() =>
            AuditLogEntry.ForSupportReply(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                Guid.NewGuid(), default));
    }
}