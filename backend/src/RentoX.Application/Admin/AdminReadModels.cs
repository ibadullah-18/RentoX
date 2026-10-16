using RentoX.Application.Common;

namespace RentoX.Application.Admin;

/// <summary>A name in one language (1 Azerbaijani, 2 Russian, 3 English).</summary>
public sealed record AdminTranslation(int Language, string Text);

public sealed record AdminCategoryResult(
    Guid Id,
    Guid? ParentId,
    string Slug,
    string? IconUrl,
    int DisplayOrder,
    bool IsActive,
    IReadOnlyList<int> RentalPeriods,
    IReadOnlyList<AdminTranslation> Translations);

public sealed record AdminFieldOptionResult(
    Guid Id,
    string Value,
    int DisplayOrder,
    bool IsActive,
    IReadOnlyList<AdminTranslation> Translations);

public sealed record AdminFieldResult(
    Guid Id,
    Guid CategoryId,
    string Key,
    int Type,
    bool IsRequired,
    bool IsFilterable,
    bool IsSearchable,
    bool AllowCustomValue,
    bool AppliesToDescendants,
    int DisplayOrder,
    bool IsActive,
    IReadOnlyList<AdminTranslation> Translations,
    IReadOnlyList<AdminFieldOptionResult> Options);

/// <summary>What an admin sees when reading the catalogue: also the inactive parts and every translation.</summary>
public interface IAdminCatalogReadService
{
    Task<IReadOnlyList<AdminCategoryResult>> GetCategoriesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>The fields defined on one category itself (not the inherited ones), inactive included.</summary>
    Task<IReadOnlyList<AdminFieldResult>> GetFieldsAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);
}

public sealed record AdminListingSearchItem(
    Guid Id,
    Guid OwnerId,
    string OwnerName,
    string? OwnerPhone,
    Guid CategoryId,
    string Title,
    decimal Price,
    string Currency,
    int RentalPeriodUnit,
    int Status,
    Guid? CoverImageId,
    int ImageCount,
    long ViewCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc);

/// <summary>All listings, whatever their status, found by status, text (title, owner name or phone) and category.</summary>
public interface IAdminListingSearchService
{
    Task<PagedResult<AdminListingSearchItem>> SearchAsync(
        int? status,
        string? text,
        Guid? categoryId,
        Guid? ownerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

public sealed record AdminUserItem(
    Guid Id,
    string FullName,
    string? Phone,
    int Status,
    DateTimeOffset? RegisteredAtUtc,
    DateTimeOffset? LastSeenAtUtc,
    int ListingCount,
    int ActiveListingCount,
    decimal Balance,
    string Currency);

public sealed record AdminWalletTransactionItem(
    Guid Id,
    int Direction,
    int Type,
    decimal Amount,
    decimal BalanceAfter,
    string Reason,
    DateTimeOffset OccurredAtUtc);

/// <param name="Kind">"listing" or "store" (a report about something they own), "conversation" (a report about a chat with them).</param>
public sealed record AdminUserReportItem(
    string Kind,
    Guid Id,
    int Reason,
    int Status,
    DateTimeOffset CreatedAtUtc,
    string? Note);

/// <summary>What others reported about this person: how many, how they ended, and the latest ones.</summary>
public sealed record AdminUserReportSummary(
    int Total,
    int Open,
    int Upheld,
    int Dismissed,
    IReadOnlyList<AdminUserReportItem> Recent);

public sealed record AdminUserDetails(
    AdminUserItem User,
    string? Bio,
    int PreferredLanguage,
    string? StatusReason,
    IReadOnlyList<AdminWalletTransactionItem> RecentTransactions,
    AdminUserReportSummary? Reports = null);

/// <summary>Reading users for the admin: who they are, their listings count, their balance and recent money moves.</summary>
public interface IAdminUserService
{
    Task<PagedResult<AdminUserItem>> SearchAsync(
        string? text,
        int? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AdminUserDetails?> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed record AdminStoreItem(
    Guid Id,
    Guid OwnerId,
    string OwnerName,
    string? OwnerPhone,
    string Name,
    string Slug,
    int Status,
    string? RejectionReason,
    DateTimeOffset CreatedAtUtc,
    long Followers = 0,
    long ActiveListings = 0,
    long ProfileViews = 0);

/// <summary>All stores, whatever their status, found by status and text (name, owner name or phone).</summary>
public interface IAdminStoreSearchService
{
    Task<PagedResult<AdminStoreItem>> SearchAsync(
        int? status,
        string? text,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

public sealed record StaffMember(
    Guid UserId,
    string FullName,
    string? Phone,
    string Role,
    IReadOnlyList<string> Sections,
    int ActiveSessions = 0,
    DateTimeOffset? LastActiveAtUtc = null);

/// <summary>Who may use the admin panel. Only a SuperAdmin calls this; it never touches SuperAdmins.</summary>
public interface IStaffManagementService
{
    Task<IReadOnlyList<StaffMember>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Gives the Admin role to an existing account, found by its phone number.</summary>
    Task<StaffMember> GrantAdminAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Limits an Admin to some sections of the panel (an empty list: all of them).
    /// The person signs in again to get the new limits.
    /// </summary>
    Task SetSectionsAsync(Guid userId, IReadOnlyList<string> sections, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ends every session of a staff member (an Admin or another SuperAdmin; not your own): their
    /// refresh tokens stop working, so they must sign in again. A token already issued still works for
    /// up to its 15 minutes.
    /// </summary>
    /// <returns>How many sessions were ended.</returns>
    Task<int> EndSessionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Takes the Admin role away and ends the person's sessions.</summary>
    Task RevokeAdminAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Whether an account may still use the API. Asked on every request, so the
/// answer is kept for a few seconds; blocking an account clears it at once.
/// </summary>
public interface IUserStatusGuard
{
    Task<bool> IsAllowedAsync(Guid userId, CancellationToken cancellationToken = default);

    void Forget(Guid userId);
}

/// <summary>Blocking and unblocking accounts.</summary>
public interface IUserModerationService
{
    /// <summary>
    /// Blocks the account: no sign-in, sessions ended, its listings taken down
    /// and its store suspended. Staff accounts and the caller cannot be blocked.
    /// </summary>
    Task BlockAsync(Guid userId, string reason, CancellationToken cancellationToken = default);

    /// <summary>Lets the person in again. Taken-down listings stay down: the owner fixes and resubmits them.</summary>
    Task UnblockAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record AnnouncementResult(int Recipients);

/// <summary>One message that was sent, with how many people got it and how many have read it.</summary>
public sealed record AnnouncementHistoryItem(
    string Title,
    string Body,
    string? ActionUrl,
    DateTimeOffset SentAtUtc,
    int Recipients,
    int Read);

/// <summary>Who gets a message when it is not written to one person.</summary>
public enum AnnouncementAudience
{
    Everyone = 1,
    StoreOwners = 2,
    Sellers = 3
}

/// <summary>Messages from the staff to people: one person, or everyone with an active account.</summary>
public interface IAnnouncementService
{
    /// <param name="userId">The one person to write to; null means the <paramref name="audience"/>.</param>
    Task<PagedResult<AnnouncementHistoryItem>> GetHistoryAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AnnouncementResult> SendAsync(
        Guid? userId,
        AnnouncementAudience audience,
        string title,
        string body,
        string? actionUrl,
        CancellationToken cancellationToken = default);
}

/// <param name="AlreadyDone">True when the same request was already carried out (a double click): nothing new was written.</param>
public sealed record WalletAdjustmentResult(decimal Amount, decimal BalanceAfter, bool AlreadyDone);

/// <summary>Money added to or taken from a person's wallet by hand: a bonus, a refund or a correction.</summary>
public interface IWalletAdjustmentService
{
    /// <param name="requestId">One per submitted form; sending it twice changes the wallet once.</param>
    /// <param name="type">Credit: Bonus (5), Refund (4) or AdminAdjustment (6). Debit: AdminAdjustment (6) only.</param>
    Task<WalletAdjustmentResult> AdjustAsync(
        Guid userId,
        Guid requestId,
        bool credit,
        int type,
        decimal amount,
        string reason,
        CancellationToken cancellationToken = default);
}

public sealed record BannedWordItem(Guid Id, string Word, Guid CreatedByUserId, DateTimeOffset CreatedAtUtc);

/// <summary>The words that mark a listing for the moderators, and which listings contain them.</summary>
public interface IBannedWordService
{
    Task<IReadOnlyList<BannedWordItem>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a word or phrase (a trailing "*" matches every ending).</summary>
    Task<BannedWordItem> AddAsync(string word, CancellationToken cancellationToken = default);

    Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>For each listing (title and description) the banned words it contains; listings without any are left out.</summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> FindInListingsAsync(
        IReadOnlyCollection<Guid> listingIds,
        CancellationToken cancellationToken = default);
}
