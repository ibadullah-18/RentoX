using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Auditing;
using RentoX.Domain.Auditing;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Support;
using RentoX.Domain.Users;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Auditing;

public sealed class AuditLogQueryService(
    RentoXDbContext dbContext,
    IClock clock)
    : IAuditLogQueryService
{
    public async Task<AuditLogPageResult> SearchAsync(
        AuditLogSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Page is < 1 or > 1000)
        {
            throw new DomainException(
                "Page must be between 1 and 1000. Narrow the filters for older records.");
        }

        if (query.PageSize is < 1 or > 100)
        {
            throw new DomainException(
                "Page size must be between 1 and 100.");
        }

        ValidateOptionalId(query.ActorUserId);
        ValidateOptionalId(query.TargetId);
        ValidateOptionalId(query.OperationId);

        if (query.Action.HasValue &&
            !Enum.IsDefined((AuditAction)query.Action.Value))
        {
            throw new DomainException(
                "Audit action is invalid.");
        }

        if (query.FromUtc.HasValue != query.ToUtc.HasValue)
        {
            throw new DomainException(
                "Provide both fromUtc and toUtc, or omit both.");
        }

        DateTimeOffset toUtc =
            (query.ToUtc ?? clock.UtcNow).ToUniversalTime();

        DateTimeOffset fromUtc =
            query.FromUtc.HasValue
                ? query.FromUtc.Value.ToUniversalTime()
                : toUtc.AddDays(-30);

        if (fromUtc >= toUtc)
        {
            throw new DomainException(
                "fromUtc must be earlier than toUtc.");
        }

        if (toUtc - fromUtc > TimeSpan.FromDays(90))
        {
            throw new DomainException(
                "The audit search interval cannot exceed 90 days.");
        }

        IQueryable<AuditLogEntry> entries =
            dbContext.Set<AuditLogEntry>()
                .AsNoTracking()
                .Where(entry =>
                    entry.OccurredAtUtc >= fromUtc &&
                    entry.OccurredAtUtc < toUtc);

        if (query.ActorUserId.HasValue)
        {
            Guid actorUserId = query.ActorUserId.Value;

            entries = entries.Where(entry =>
                entry.ActorUserId == actorUserId);
        }

        if (query.TargetId.HasValue)
        {
            Guid targetId = query.TargetId.Value;

            entries = entries.Where(entry =>
                entry.TargetId == targetId);
        }

        if (query.OperationId.HasValue)
        {
            Guid operationId = query.OperationId.Value;

            entries = entries.Where(entry =>
                entry.OperationId == operationId);
        }

        if (query.Action.HasValue)
        {
            AuditAction action = (AuditAction)query.Action.Value;

            entries = entries.Where(entry =>
                entry.Action == action);
        }

        int totalCount =
            await entries.CountAsync(cancellationToken);

        int offset = (query.Page - 1) * query.PageSize;

        AuditLogItemResult[] items =
            await entries
                .OrderByDescending(entry => entry.OccurredAtUtc)
                .ThenByDescending(entry => entry.Id)
                .Skip(offset)
                .Take(query.PageSize)
                .Select(entry => new AuditLogItemResult(
                    entry.Id,
                    entry.OperationId,
                    entry.ActorUserId,
                    entry.TargetId,
                    (int)entry.Action,
                    entry.PreviousValue,
                    entry.CurrentValue,
                    entry.RelatedEntityId,
                    entry.OccurredAtUtc))
                .ToArrayAsync(cancellationToken);

        items = await AddNamesAsync(items, cancellationToken);

        int totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)query.PageSize);

        return new AuditLogPageResult(
            items,
            query.Page,
            query.PageSize,
            totalCount,
            totalPages,
            fromUtc,
            toUtc);
    }

    /// <summary>Puts a readable name next to each person and thing in the list (the audit rows only hold ids).</summary>
    private async Task<AuditLogItemResult[]> AddNamesAsync(
        AuditLogItemResult[] items,
        CancellationToken cancellationToken)
    {
        if (items.Length == 0)
        {
            return items;
        }

        // Which table an id belongs to depends on the action.
        static bool IsListing(int a) => a == (int)AuditAction.ListingStatusChanged;
        static bool IsStore(int a) => a == (int)AuditAction.StoreStatusChanged;
        static bool IsTicket(int a) =>
            a is (int)AuditAction.SupportStatusChanged
                or (int)AuditAction.SupportPriorityChanged
                or (int)AuditAction.SupportReplyAdded;
        static bool IsUser(int a) =>
            a is (int)AuditAction.StaffRoleChanged
                or (int)AuditAction.UserStatusChanged
                or (int)AuditAction.StaffSectionsChanged;

        Guid[] listingIds = items.Where(i => IsListing(i.Action)).Select(i => i.TargetId).Distinct().ToArray();
        Guid[] storeIds = items.Where(i => IsStore(i.Action)).Select(i => i.TargetId).Distinct().ToArray();
        Guid[] ticketIds = items.Where(i => IsTicket(i.Action)).Select(i => i.TargetId).Distinct().ToArray();
        Guid[] personIds = items
            .Select(i => i.ActorUserId)
            .Concat(items.Where(i => IsUser(i.Action)).Select(i => i.TargetId))
            .Distinct()
            .ToArray();

        Dictionary<Guid, string> listings = listingIds.Length == 0
            ? []
            : await dbContext.Listings.IgnoreQueryFilters().AsNoTracking()
                .Where(l => listingIds.Contains(l.Id))
                .ToDictionaryAsync(l => l.Id, l => l.Title, cancellationToken);

        Dictionary<Guid, string> stores = storeIds.Length == 0
            ? []
            : await dbContext.StoreProfiles.IgnoreQueryFilters().AsNoTracking()
                .Where(s => storeIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        Dictionary<Guid, string> tickets = ticketIds.Length == 0
            ? []
            : await dbContext.Set<SupportTicket>().AsNoTracking()
                .Where(t => ticketIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Subject, cancellationToken);

        Dictionary<Guid, string> people = await dbContext.Set<UserProfile>().AsNoTracking()
            .Where(p => personIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.FullName, cancellationToken);

        static string? Find(Dictionary<Guid, string> names, Guid id) => names.GetValueOrDefault(id);

        return items.Select(i => i with
        {
            ActorName = Find(people, i.ActorUserId),
            TargetName = IsListing(i.Action) ? Find(listings, i.TargetId)
                : IsStore(i.Action) ? Find(stores, i.TargetId)
                : IsTicket(i.Action) ? Find(tickets, i.TargetId)
                : IsUser(i.Action) ? Find(people, i.TargetId)
                : null
        }).ToArray();
    }

    private static void ValidateOptionalId(Guid? value)
    {
        if (value.HasValue && value.Value == Guid.Empty)
        {
            throw new DomainException(
                "An audit filter id cannot be empty.");
        }
    }
}