using Microsoft.EntityFrameworkCore;
using Npgsql;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Common;
using RentoX.Application.Moderation;
using RentoX.Application.Notifications;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Moderation;
using RentoX.Domain.Notifications;
using RentoX.Domain.Stores;
using RentoX.Domain.Stores.Enums;
using RentoX.Domain.Users;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Moderation;

public sealed class ContentReportService(
    RentoXDbContext dbContext,
    IClock clock,
    RentoX.Application.Support.ISupportEventPublisher? live = null,
    INotificationService? notifications = null)
    : IContentReportService
{
    private const int MaximumPageSize = 50;

    public async Task<ContentReportResult?> CreateAsync(
        Guid reporterId,
        int targetType,
        Guid targetId,
        int reason,
        string? details,
        CancellationToken cancellationToken = default)
    {
        if (reporterId == Guid.Empty)
        {
            throw new DomainException("Reporter id is required.");
        }

        if (!Enum.IsDefined(typeof(ContentReportTarget), targetType))
        {
            throw new DomainException("Report target is invalid.");
        }

        if (!Enum.IsDefined(typeof(ContentReportReason), reason))
        {
            throw new DomainException("Report reason is invalid.");
        }

        ContentReportTarget target = (ContentReportTarget)targetType;
        DateTimeOffset now = clock.UtcNow;

        Guid? ownerId = await FindOwnerAsync(
            target, targetId, now, cancellationToken);

        if (!ownerId.HasValue)
        {
            return null;
        }

        if (ownerId.Value == reporterId)
        {
            throw new DomainException("You cannot report your own content.");
        }

        bool alreadyReported =
            await dbContext.Set<ContentReport>()
                .AsNoTracking()
                .AnyAsync(
                    report =>
                        report.TargetType == target &&
                        report.TargetId == targetId &&
                        report.ReporterId == reporterId &&
                        (report.Status == ContentReportStatus.Pending ||
                         report.Status == ContentReportStatus.Reviewing),
                    cancellationToken);

        if (alreadyReported)
        {
            throw new DomainException(
                "You have already reported this. We are reviewing it.");
        }

        ContentReport report = ContentReport.Create(
            target,
            targetId,
            reporterId,
            (ContentReportReason)reason,
            details,
            now);

        dbContext.Set<ContentReport>().Add(report);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            })
        {
            throw new DomainException(
                "You have already reported this. We are reviewing it.");
        }

        if (live is not null)
        {
            await live.ReportCreatedAsync("content", report.Id, cancellationToken);
        }

        return Map(report);
    }

    public async Task<PagedResult<ContentReportSummaryResult>>
        GetForAdminAsync(
            int? status,
            int? targetType,
            int page,
            int pageSize,
            Guid? targetId = null,
            CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            pageSize,
            MaximumPageSize);

        IQueryable<ContentReport> query =
            dbContext.Set<ContentReport>().AsNoTracking();

        if (status.HasValue)
        {
            ContentReportStatus wanted = (ContentReportStatus)status.Value;
            query = query.Where(report => report.Status == wanted);
        }

        if (targetType.HasValue)
        {
            ContentReportTarget wanted = (ContentReportTarget)targetType.Value;
            query = query.Where(report => report.TargetType == wanted);
        }

        if (targetId.HasValue)
        {
            Guid wanted = targetId.Value;
            query = query.Where(report => report.TargetId == wanted);
        }

        int total = await query.CountAsync(cancellationToken);

        // The queue is worked from the oldest: those have waited the longest. A closed list is read from the
        // newest. With no status chosen, the open ones come first, oldest first.
        bool closed = status is (int)ContentReportStatus.Resolved or (int)ContentReportStatus.Rejected;
        IOrderedQueryable<ContentReport> ordered = closed
            ? query.OrderByDescending(report => report.CreatedAtUtc)
            : query.OrderBy(report => report.Status).ThenBy(report => report.CreatedAtUtc);

        var rows =
            await ordered
                .ThenBy(report => report.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

        Dictionary<Guid, string> titles =
            await TitlesAsync(rows, cancellationToken);
        Dictionary<Guid, TargetFacts> facts =
            await FactsAsync(rows, cancellationToken);
        Dictionary<Guid, string> names =
            await NamesAsync(
                rows.Select(report => report.ReporterId)
                    .Concat(facts.Values.Select(fact => fact.OwnerId))
                    .Distinct()
                    .ToArray(),
                cancellationToken);

        Guid[] targetIds = rows.Select(report => report.TargetId).Distinct().ToArray();
        Dictionary<Guid, int> openByTarget =
            await dbContext.Set<ContentReport>()
                .AsNoTracking()
                .Where(report =>
                    targetIds.Contains(report.TargetId) &&
                    (report.Status == ContentReportStatus.Pending ||
                     report.Status == ContentReportStatus.Reviewing))
                .GroupBy(report => report.TargetId)
                .Select(group => new { group.Key, Count = group.Count() })
                .ToDictionaryAsync(row => row.Key, row => row.Count, cancellationToken);

        List<ContentReportSummaryResult> items =
            rows
                .Select(report =>
                {
                    TargetFacts? fact = facts.GetValueOrDefault(report.TargetId);
                    return new ContentReportSummaryResult(
                        report.Id,
                        (int)report.TargetType,
                        report.TargetId,
                        titles.GetValueOrDefault(report.TargetId),
                        report.ReporterId,
                        (int)report.Reason,
                        (int)report.Status,
                        report.CreatedAtUtc,
                        names.GetValueOrDefault(report.ReporterId),
                        fact?.OwnerId,
                        fact is null ? null : names.GetValueOrDefault(fact.OwnerId),
                        openByTarget.GetValueOrDefault(report.TargetId),
                        fact?.Status,
                        fact?.CoverImageId);
                })
                .ToList();

        return new PagedResult<ContentReportSummaryResult>(
            items,
            page,
            pageSize,
            total,
            total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<ContentReportDetailsResult?> GetDetailsAsync(
        Guid reportId,
        CancellationToken cancellationToken = default)
    {
        ContentReport? report =
            await dbContext.Set<ContentReport>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.Id == reportId,
                    cancellationToken);

        if (report is null)
        {
            return null;
        }

        Dictionary<Guid, string> titles =
            await TitlesAsync([report], cancellationToken);

        Guid? ownerId = await FindOwnerAsync(
            report.TargetType,
            report.TargetId,
            null,
            cancellationToken);

        int open =
            await dbContext.Set<ContentReport>()
                .AsNoTracking()
                .CountAsync(
                    item =>
                        item.TargetType == report.TargetType &&
                        item.TargetId == report.TargetId &&
                        (item.Status == ContentReportStatus.Pending ||
                         item.Status == ContentReportStatus.Reviewing),
                    cancellationToken);

        Dictionary<Guid, TargetFacts> facts =
            await FactsAsync([report], cancellationToken);
        TargetFacts? fact = facts.GetValueOrDefault(report.TargetId);
        Dictionary<Guid, string> names =
            await NamesAsync(
                new[] { report.ReporterId }
                    .Concat(fact is null ? [] : [fact.OwnerId])
                    .Distinct()
                    .ToArray(),
                cancellationToken);

        return new ContentReportDetailsResult(
            report.Id,
            (int)report.TargetType,
            report.TargetId,
            titles.GetValueOrDefault(report.TargetId),
            ownerId,
            report.ReporterId,
            (int)report.Reason,
            report.Details,
            (int)report.Status,
            report.CreatedAtUtc,
            report.ReviewedByUserId,
            report.ReviewedAtUtc,
            report.ResolutionNote,
            open,
            names.GetValueOrDefault(report.ReporterId),
            fact is null ? null : names.GetValueOrDefault(fact.OwnerId),
            fact?.Status,
            fact?.CoverImageId);
    }

    public async Task<ContentReportResult?> ReviewAsync(
        Guid reviewerId,
        Guid reportId,
        int status,
        string? resolutionNote,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(typeof(ContentReportStatus), status))
        {
            throw new DomainException("Report status is invalid.");
        }

        ContentReport? report =
            await dbContext.Set<ContentReport>()
                .SingleOrDefaultAsync(
                    item => item.Id == reportId,
                    cancellationToken);

        if (report is null)
        {
            return null;
        }

        report.Review(
            (ContentReportStatus)status,
            reviewerId,
            resolutionNote,
            clock.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        // The person who reported is told what became of it (the same as for a conversation report).
        if (notifications is not null)
        {
            string title = report.Status switch
            {
                ContentReportStatus.Reviewing => "Şikayət araşdırılır",
                ContentReportStatus.Resolved => "Şikayət həll edildi",
                ContentReportStatus.Rejected => "Şikayət rədd edildi",
                _ => "Şikayət statusu yeniləndi"
            };

            string actionUrl = report.TargetType == ContentReportTarget.Listing
                ? $"/listings/{report.TargetId:D}"
                : $"/stores/{report.TargetId:D}";

            await notifications.CreateAsync(
                new CreateNotificationCommand(
                    report.ReporterId,
                    (int)NotificationType.ConversationReportUpdated,
                    title,
                    report.ResolutionNote ?? "Göndərdiyiniz şikayət araşdırmaya götürüldü.",
                    report.TargetId,
                    actionUrl),
                cancellationToken);
        }

        return Map(report);
    }

    // The owner of a reportable (live) target, or null when there is none.
    // `now` null = look the target up even if it is no longer public (the
    // moderator may still want to see it).
    private async Task<Guid?> FindOwnerAsync(
        ContentReportTarget target,
        Guid targetId,
        DateTimeOffset? now,
        CancellationToken cancellationToken)
    {
        if (target == ContentReportTarget.Listing)
        {
            return await dbContext.Listings
                .AsNoTracking()
                .Where(listing =>
                    listing.Id == targetId &&
                    (now == null ||
                     (listing.Status == ListingStatus.Active &&
                      listing.ExpiresAtUtc > now)))
                .Select(listing => (Guid?)listing.OwnerId)
                .SingleOrDefaultAsync(cancellationToken);
        }

        return await dbContext.Set<StoreProfile>()
            .AsNoTracking()
            .Where(store =>
                store.Id == targetId &&
                (now == null || store.Status == StoreStatus.Active))
            .Select(store => (Guid?)store.OwnerId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private sealed record TargetFacts(Guid OwnerId, int Status, Guid? CoverImageId);

    /// <summary>Who owns each reported thing, what state it is in, and (for a listing) its cover picture.</summary>
    private async Task<Dictionary<Guid, TargetFacts>> FactsAsync(
        IReadOnlyCollection<ContentReport> reports,
        CancellationToken cancellationToken)
    {
        Guid[] listingIds = reports.Where(r => r.TargetType == ContentReportTarget.Listing).Select(r => r.TargetId).ToArray();
        Guid[] storeIds = reports.Where(r => r.TargetType == ContentReportTarget.Store).Select(r => r.TargetId).ToArray();
        Dictionary<Guid, TargetFacts> facts = [];

        if (listingIds.Length > 0)
        {
            foreach (var row in await dbContext.Listings
                         .IgnoreQueryFilters()
                         .AsNoTracking()
                         .Where(listing => listingIds.Contains(listing.Id))
                         .Select(listing => new
                         {
                             listing.Id,
                             listing.OwnerId,
                             Status = (int)listing.Status,
                             Cover = listing.Images.Where(image => image.IsCover)
                                 .Select(image => (Guid?)image.Id).FirstOrDefault()
                         })
                         .ToListAsync(cancellationToken))
            {
                facts[row.Id] = new TargetFacts(row.OwnerId, row.Status, row.Cover);
            }
        }

        if (storeIds.Length > 0)
        {
            foreach (var row in await dbContext.Set<StoreProfile>()
                         .IgnoreQueryFilters()
                         .AsNoTracking()
                         .Where(store => storeIds.Contains(store.Id))
                         .Select(store => new { store.Id, store.OwnerId, Status = (int)store.Status })
                         .ToListAsync(cancellationToken))
            {
                facts[row.Id] = new TargetFacts(row.OwnerId, row.Status, null);
            }
        }

        return facts;
    }

    private async Task<Dictionary<Guid, string>> NamesAsync(
        Guid[] userIds,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<UserProfile>()
            .AsNoTracking()
            .Where(profile => userIds.Contains(profile.Id))
            .ToDictionaryAsync(profile => profile.Id, profile => profile.FullName, cancellationToken);
    }

    private async Task<Dictionary<Guid, string>> TitlesAsync(
        IReadOnlyCollection<ContentReport> reports,
        CancellationToken cancellationToken)
    {
        Guid[] listingIds =
            reports
                .Where(report => report.TargetType == ContentReportTarget.Listing)
                .Select(report => report.TargetId)
                .ToArray();

        Guid[] storeIds =
            reports
                .Where(report => report.TargetType == ContentReportTarget.Store)
                .Select(report => report.TargetId)
                .ToArray();

        Dictionary<Guid, string> titles = [];

        if (listingIds.Length > 0)
        {
            foreach (var row in await dbContext.Listings
                         .IgnoreQueryFilters()
                         .AsNoTracking()
                         .Where(listing => listingIds.Contains(listing.Id))
                         .Select(listing => new { listing.Id, listing.Title })
                         .ToListAsync(cancellationToken))
            {
                titles[row.Id] = row.Title;
            }
        }

        if (storeIds.Length > 0)
        {
            foreach (var row in await dbContext.Set<StoreProfile>()
                         .IgnoreQueryFilters()
                         .AsNoTracking()
                         .Where(store => storeIds.Contains(store.Id))
                         .Select(store => new { store.Id, store.Name })
                         .ToListAsync(cancellationToken))
            {
                titles[row.Id] = row.Name;
            }
        }

        return titles;
    }

    private static ContentReportResult Map(ContentReport report) =>
        new(
            report.Id,
            (int)report.TargetType,
            report.TargetId,
            (int)report.Reason,
            report.Details,
            (int)report.Status,
            report.CreatedAtUtc);
}
