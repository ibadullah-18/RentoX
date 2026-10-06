using System.Globalization;
using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;
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