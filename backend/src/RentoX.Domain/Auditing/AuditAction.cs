namespace RentoX.Domain.Auditing;

// Persisted values: do not renumber existing entries.
public enum AuditAction
{
    ListingStatusChanged = 1,
    StoreStatusChanged = 2,
    SupportStatusChanged = 3,
    SupportPriorityChanged = 4,
    SupportReplyAdded = 5,
    StaffRoleChanged = 6,
    UserStatusChanged = 7,
    StaffSectionsChanged = 8,
    AnnouncementSent = 9,
    StaffSessionsEnded = 10,
    WalletAdjusted = 11,

    /// <summary>
    /// The owner changed something about a staff member: the current value says what (1 profile, 2 access,
    /// 3 pay line added, 4 pay line voided, 5 day off decided, 6 request handled); the related id is the record.
    /// </summary>
    StaffHrChanged = 12,

    /// <summary>
    /// A free (gift) VIP or bump was given: the current value says what (1 listing bump, 2 listing VIP, 3 VIP store);
    /// the target is the person who received it; the related id is the promotion or the store VIP period.
    /// </summary>
    GiftGranted = 13
}