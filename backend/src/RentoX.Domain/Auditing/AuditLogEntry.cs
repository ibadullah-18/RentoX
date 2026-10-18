using System.Globalization;
using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Users.Enums;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Stores.Enums;
using RentoX.Domain.Support;

namespace RentoX.Domain.Auditing;

public sealed class AuditLogEntry : Entity
{
    private AuditLogEntry()
    {
    }

    private AuditLogEntry(
        Guid operationId,
        Guid actorUserId,
        Guid targetId,
        AuditAction action,
        int? previousValue,
        int? currentValue,
        Guid? relatedEntityId,
        DateTimeOffset occurredAtUtc)
        : base(Guid.NewGuid())
    {
        RequireId(operationId, "Audit operation id");
        RequireId(actorUserId, "Audit actor id");
        RequireId(targetId, "Audit target id");

        if (occurredAtUtc == default ||
            occurredAtUtc.Offset != TimeSpan.Zero)
        {
            throw new DomainException(
                "Audit occurrence time must be a non-default UTC value.");
        }

        OperationId = operationId;
        ActorUserId = actorUserId;
        TargetId = targetId;
        Action = action;
        PreviousValue = previousValue;
        CurrentValue = currentValue;
        RelatedEntityId = relatedEntityId;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid OperationId { get; private set; }

    public Guid ActorUserId { get; private set; }

    public Guid TargetId { get; private set; }

    public AuditAction Action { get; private set; }

    public int? PreviousValue { get; private set; }

    public int? CurrentValue { get; private set; }

    public Guid? RelatedEntityId { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static AuditLogEntry ForListingStatusChange(
        Guid operationId,
        Guid actorUserId,
        Guid listingId,
        ListingStatus previousStatus,
        ListingStatus currentStatus,
        DateTimeOffset occurredAtUtc)
    {
        return CreateChange(
            operationId, actorUserId, listingId,
            AuditAction.ListingStatusChanged,
            previousStatus, currentStatus, occurredAtUtc);
    }

    public static AuditLogEntry ForStoreStatusChange(
        Guid operationId,
        Guid actorUserId,
        Guid storeId,
        StoreStatus previousStatus,
        StoreStatus currentStatus,
        DateTimeOffset occurredAtUtc)
    {
        return CreateChange(
            operationId, actorUserId, storeId,
            AuditAction.StoreStatusChanged,
            previousStatus, currentStatus, occurredAtUtc);
    }

    public static AuditLogEntry ForSupportStatusChange(
        Guid operationId,
        Guid actorUserId,
        Guid ticketId,
        SupportTicketStatus previousStatus,
        SupportTicketStatus currentStatus,
        DateTimeOffset occurredAtUtc)
    {
        return CreateChange(
            operationId, actorUserId, ticketId,
            AuditAction.SupportStatusChanged,
            previousStatus, currentStatus, occurredAtUtc);
    }

    public static AuditLogEntry ForSupportPriorityChange(
        Guid operationId,
        Guid actorUserId,
        Guid ticketId,
        SupportTicketPriority previousPriority,
        SupportTicketPriority currentPriority,
        DateTimeOffset occurredAtUtc)
    {
        return CreateChange(
            operationId, actorUserId, ticketId,
            AuditAction.SupportPriorityChanged,
            previousPriority, currentPriority, occurredAtUtc);
    }

    public static AuditLogEntry ForSupportReply(
        Guid operationId,
        Guid actorUserId,
        Guid ticketId,
        Guid messageId,
        DateTimeOffset occurredAtUtc)
    {
        RequireId(messageId, "Audit support message id");

        return new AuditLogEntry(
            operationId,
            actorUserId,
            ticketId,
            AuditAction.SupportReplyAdded,
            null,
            null,
            messageId,
            occurredAtUtc);
    }

    public static AuditLogEntry ForUserStatusChange(
        Guid operationId,
        Guid actorUserId,
        Guid userId,
        UserStatus previousStatus,
        UserStatus currentStatus,
        DateTimeOffset occurredAtUtc)
    {
        return CreateChange(
            operationId, actorUserId, userId,
            AuditAction.UserStatusChanged,
            previousStatus, currentStatus, occurredAtUtc);
    }

    /// <summary>The sections of the panel an Admin may use changed; both values are sets written as bits.</summary>
    public static AuditLogEntry ForStaffSectionsChange(
        Guid operationId,
        Guid actorUserId,
        Guid targetUserId,
        int previousMask,
        int currentMask,
        DateTimeOffset occurredAtUtc)
    {
        return new AuditLogEntry(
            operationId,
            actorUserId,
            targetUserId,
            AuditAction.StaffSectionsChanged,
            previousMask,
            currentMask,
            null,
            occurredAtUtc);
    }

    /// <summary>A message was sent to people from the panel; the target is the single recipient or, for everyone, the operation.</summary>
    public static AuditLogEntry ForAnnouncement(
        Guid operationId,
        Guid actorUserId,
        Guid targetId,
        int recipientCount,
        DateTimeOffset occurredAtUtc)
    {
        if (recipientCount < 1)
        {
            throw new DomainException("An announcement needs at least one recipient.");
        }

        return new AuditLogEntry(
            operationId,
            actorUserId,
            targetId,
            AuditAction.AnnouncementSent,
            null,
            recipientCount,
            null,
            occurredAtUtc);
    }

    /// <summary>A SuperAdmin ended every session of a staff member; the current value is how many there were.</summary>
    public static AuditLogEntry ForStaffSessionsEnded(
        Guid operationId,
        Guid actorUserId,
        Guid targetUserId,
        int endedCount,
        DateTimeOffset occurredAtUtc)
    {
        if (endedCount < 0)
        {
            throw new DomainException("The number of ended sessions cannot be negative.");
        }

        return new AuditLogEntry(
            operationId,
            actorUserId,
            targetUserId,
            AuditAction.StaffSessionsEnded,
            null,
            endedCount,
            null,
            occurredAtUtc);
    }

    /// <summary>
    /// A staff member added money to (positive) or took money from (negative) a wallet by hand.
    /// The current value is the amount in qəpik (1/100 AZN); the related id is the wallet transaction.
    /// </summary>
    public static AuditLogEntry ForWalletAdjustment(
        Guid operationId,
        Guid actorUserId,
        Guid targetUserId,
        int signedAmountInQepik,
        Guid walletTransactionId,
        DateTimeOffset occurredAtUtc)
    {
        if (signedAmountInQepik == 0)
        {
            throw new DomainException("A wallet adjustment cannot be zero.");
        }

        RequireId(walletTransactionId, "Audit wallet transaction id");

        return new AuditLogEntry(
            operationId,
            actorUserId,
            targetUserId,
            AuditAction.WalletAdjusted,
            null,
            signedAmountInQepik,
            walletTransactionId,
            occurredAtUtc);
    }

    /// <summary>Something about a staff member's pay, access or days off was changed by the owner.</summary>
    public static AuditLogEntry ForStaffHr(
        Guid operationId,
        Guid actorUserId,
        Guid staffUserId,
        int changeCode,
        Guid relatedEntityId,
        DateTimeOffset occurredAtUtc)
    {
        if (changeCode is < 1 or > 20)
        {
            throw new DomainException("The kind of staff change is not valid.");
        }

        RequireId(relatedEntityId, "Audit staff record id");

        return new AuditLogEntry(
            operationId,
            actorUserId,
            staffUserId,
            AuditAction.StaffHrChanged,
            null,
            changeCode,
            relatedEntityId,
            occurredAtUtc);
    }

    /// <summary>The owner gave a free VIP or bump (1 listing bump, 2 listing VIP, 3 VIP store) to a person; 4 and 5 = all of the person's services switched free on / off.</summary>
    public static AuditLogEntry ForGift(
        Guid operationId,
        Guid actorUserId,
        Guid recipientUserId,
        int kind,
        Guid relatedEntityId,
        DateTimeOffset occurredAtUtc)
    {
        if (kind is < 1 or > 5)
        {
            throw new DomainException("The kind of gift is not valid.");
        }

        RequireId(relatedEntityId, "Audit gift record id");

        return new AuditLogEntry(
            operationId,
            actorUserId,
            recipientUserId,
            AuditAction.GiftGranted,
            null,
            kind,
            relatedEntityId,
            occurredAtUtc);
    }

    /// <summary>An admin role was given (1) or taken away (0) from a person.</summary>
    public static AuditLogEntry ForStaffRoleChange(
        Guid operationId,
        Guid actorUserId,
        Guid targetUserId,
        bool granted,
        DateTimeOffset occurredAtUtc)
    {
        return new AuditLogEntry(
            operationId,
            actorUserId,
            targetUserId,
            AuditAction.StaffRoleChanged,
            granted ? 0 : 1,
            granted ? 1 : 0,
            null,
            occurredAtUtc);
    }

    private static AuditLogEntry CreateChange<TValue>(
        Guid operationId,
        Guid actorUserId,
        Guid targetId,
        AuditAction action,
        TValue previousValue,
        TValue currentValue,
        DateTimeOffset occurredAtUtc)
        where TValue : struct, Enum
    {
        if (!Enum.IsDefined(previousValue) ||
            !Enum.IsDefined(currentValue))
        {
            throw new DomainException(
                "Audit change contains an invalid enum value.");
        }

        int previous = Convert.ToInt32(
            previousValue, CultureInfo.InvariantCulture);

        int current = Convert.ToInt32(
            currentValue, CultureInfo.InvariantCulture);

        if (previous == current)
        {
            throw new DomainException(
                "An unchanged value must not create a change audit entry.");
        }

        return new AuditLogEntry(
            operationId,
            actorUserId,
            targetId,
            action,
            previous,
            current,
            null,
            occurredAtUtc);
    }

    private static void RequireId(Guid value, string field)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException($"{field} is required.");
        }
    }
}