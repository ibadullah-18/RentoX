using Microsoft.EntityFrameworkCore;
using RentoX.Application.Admin;
using RentoX.Application.Common;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Messaging;
using RentoX.Domain.Moderation;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Domain.Wallets;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Admin;

public sealed class AdminUserService(RentoXDbContext dbContext) : IAdminUserService
{
    private const int MaximumPageSize = 50;

    public async Task<PagedResult<AdminUserItem>> SearchAsync(
        string? text,
        int? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaximumPageSize);

        var query =
            from profile in dbContext.Set<UserProfile>().AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on profile.Id equals user.Id
            select new { profile, user };

        if (status is not null && Enum.IsDefined(typeof(UserStatus), status.Value))
        {
            UserStatus wanted = (UserStatus)status.Value;
            query = query.Where(x => x.profile.Status == wanted);
        }

        string? needle = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        if (needle is not null)
        {
            string pattern = "%" + needle.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            query = query.Where(x =>
                EF.Functions.ILike(x.profile.FullName, pattern, "\\") ||
                (x.user.PhoneNumber != null && EF.Functions.ILike(x.user.PhoneNumber, pattern, "\\")));
        }

        int total = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(x => x.user.RegisteredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.profile.Id,
                x.profile.FullName,
                x.user.PhoneNumber,
                Status = (int)x.profile.Status,
                x.user.RegisteredAtUtc,
                x.profile.LastSeenAtUtc
            })
            .ToListAsync(cancellationToken);

        AdminUserItem[] items = await Enrich(rows.Select(r =>
            (r.Id, r.FullName, r.PhoneNumber, r.Status, (DateTimeOffset?)r.RegisteredAtUtc, r.LastSeenAtUtc)).ToList(),
            cancellationToken);

        int totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
        return new PagedResult<AdminUserItem>(items, page, pageSize, total, totalPages);
    }

    public async Task<AdminUserDetails?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var row = await (
            from profile in dbContext.Set<UserProfile>().AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on profile.Id equals user.Id
            where profile.Id == userId
            select new
            {
                profile.Id,
                profile.FullName,
                user.PhoneNumber,
                Status = (int)profile.Status,
                user.RegisteredAtUtc,
                profile.LastSeenAtUtc,
                profile.Bio,
                profile.StatusReason,
                Language = (int)profile.PreferredLanguage
            }).SingleOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        AdminUserItem[] items = await Enrich(
            [(row.Id, row.FullName, row.PhoneNumber, row.Status, (DateTimeOffset?)row.RegisteredAtUtc, row.LastSeenAtUtc)],
            cancellationToken);

        var moves = await (
            from transaction in dbContext.Set<WalletTransaction>().AsNoTracking()
            join wallet in dbContext.Set<Wallet>().AsNoTracking() on transaction.WalletId equals wallet.Id
            where wallet.UserId == userId
            orderby transaction.OccurredAtUtc descending
            select new
            {
                transaction.Id,
                Direction = (int)transaction.Direction,
                Type = (int)transaction.Type,
                transaction.Amount,
                transaction.BalanceAfter,
                transaction.Reason,
                transaction.OccurredAtUtc
            })
            .Take(20)
            .ToListAsync(cancellationToken);

        AdminUserReportSummary reports = await ReadReportsAsync(userId, cancellationToken);

        return new AdminUserDetails(
            items[0],
            row.Bio,
            row.Language,
            row.StatusReason,
            moves.Select(m => new AdminWalletTransactionItem(
                m.Id, m.Direction, m.Type, m.Amount, m.BalanceAfter, m.Reason, m.OccurredAtUtc)).ToArray(),
            reports);
    }

    /// <summary>
    /// Reports about this person: on their listings and stores, and on chats with them
    /// (reported by the other side). Looks at the latest 200 of each kind.
    /// </summary>
    private async Task<AdminUserReportSummary> ReadReportsAsync(Guid userId, CancellationToken cancellationToken)
    {
        const int Limit = 200;

        var onListings = await (
            from report in dbContext.Set<ContentReport>().AsNoTracking()
            join listing in dbContext.Listings.IgnoreQueryFilters().AsNoTracking() on report.TargetId equals listing.Id
            where report.TargetType == ContentReportTarget.Listing && listing.OwnerId == userId
            orderby report.CreatedAtUtc descending
            select new { report.Id, Reason = (int)report.Reason, Status = (int)report.Status, report.CreatedAtUtc, Note = report.ResolutionNote })
            .Take(Limit).ToListAsync(cancellationToken);

        var onStores = await (
            from report in dbContext.Set<ContentReport>().AsNoTracking()
            join store in dbContext.StoreProfiles.IgnoreQueryFilters().AsNoTracking() on report.TargetId equals store.Id
            where report.TargetType == ContentReportTarget.Store && store.OwnerId == userId
            orderby report.CreatedAtUtc descending
            select new { report.Id, Reason = (int)report.Reason, Status = (int)report.Status, report.CreatedAtUtc, Note = report.ResolutionNote })
            .Take(Limit).ToListAsync(cancellationToken);

        var onChats = await (
            from report in dbContext.Set<ConversationReport>().AsNoTracking()
            join conversation in dbContext.Set<Conversation>().AsNoTracking() on report.ConversationId equals conversation.Id
            where report.ReporterId != userId &&
                  (conversation.BuyerId == userId || conversation.SellerId == userId)
            orderby report.CreatedAtUtc descending
            select new { report.Id, Reason = (int)report.Reason, Status = (int)report.Status, report.CreatedAtUtc, Note = report.ResolutionNote })
            .Take(Limit).ToListAsync(cancellationToken);

        List<AdminUserReportItem> all =
        [
            .. onListings.Select(r => new AdminUserReportItem("listing", r.Id, r.Reason, r.Status, r.CreatedAtUtc, r.Note)),
            .. onStores.Select(r => new AdminUserReportItem("store", r.Id, r.Reason, r.Status, r.CreatedAtUtc, r.Note)),
            .. onChats.Select(r => new AdminUserReportItem("conversation", r.Id, r.Reason, r.Status, r.CreatedAtUtc, r.Note))
        ];

        // Both kinds number the statuses alike: 1 waiting, 2 being looked at, 3 upheld, 4 dismissed.
        return new AdminUserReportSummary(
            all.Count,
            all.Count(r => r.Status is 1 or 2),
            all.Count(r => r.Status == 3),
            all.Count(r => r.Status == 4),
            all.OrderByDescending(r => r.CreatedAtUtc).Take(10).ToArray());
    }

    // Listing counts and the balance of the people on this page.
    private async Task<AdminUserItem[]> Enrich(
        List<(Guid Id, string FullName, string? Phone, int Status, DateTimeOffset? Registered, DateTimeOffset? LastSeen)> people,
        CancellationToken cancellationToken)
    {
        Guid[] ids = people.Select(p => p.Id).ToArray();

        var counts = await dbContext.Listings.AsNoTracking()
            .Where(l => ids.Contains(l.OwnerId))
            .GroupBy(l => l.OwnerId)
            .Select(g => new
            {
                Owner = g.Key,
                All = g.Count(l => l.Status != ListingStatus.Deleted),
                Active = g.Count(l => l.Status == ListingStatus.Active)
            })
            .ToListAsync(cancellationToken);

        var wallets = await dbContext.Set<Wallet>().AsNoTracking()
            .Where(w => ids.Contains(w.UserId))
            .Select(w => new { w.UserId, w.Balance, w.Currency })
            .ToListAsync(cancellationToken);

        return people.Select(p =>
        {
            var c = counts.FirstOrDefault(x => x.Owner == p.Id);
            var w = wallets.FirstOrDefault(x => x.UserId == p.Id);
            return new AdminUserItem(
                p.Id, p.FullName, p.Phone, p.Status, p.Registered, p.LastSeen,
                c?.All ?? 0, c?.Active ?? 0, w?.Balance ?? 0m, w?.Currency ?? "AZN");
        }).ToArray();
    }
}
