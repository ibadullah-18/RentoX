namespace RentoX.Domain.Auditing;

// Persisted values: do not renumber existing entries.
public enum AuditAction
{
    ListingStatusChanged = 1,
    StoreStatusChanged = 2,
    SupportStatusChanged = 3,
    SupportPriorityChanged = 4,
    SupportReplyAdded = 5
}