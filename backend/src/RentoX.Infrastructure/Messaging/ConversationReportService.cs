using Microsoft.EntityFrameworkCore;
using Npgsql;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Common;
using RentoX.Application.Files;
using RentoX.Application.Messaging;
using RentoX.Application.Notifications;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Messaging;
using RentoX.Domain.Notifications;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Messaging;

public sealed class ConversationReportService(
    RentoXDbContext dbContext,
    IClock clock,
    IFileStorage fileStorage,
    INotificationService notificationService)
    : IConversationReportService
{
    public async Task<ConversationReportResult?> CreateAsync(
        CreateConversationReportCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        RequireUser(command.UserId);

        Conversation? conversation = await dbContext.Conversations
            .AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.Id == command.ConversationId &&
                (item.BuyerId == command.UserId || item.SellerId == command.UserId),
                cancellationToken);
        if (conversation is null) { return null; }

        if (!Enum.IsDefined(typeof(ConversationReportReason), command.Reason))
        {
            throw new DomainException("Report reason is invalid.");
        }

        if (command.EvidenceMessageId.HasValue)
        {
            bool evidenceExists = await dbContext.Messages.AsNoTracking().AnyAsync(
                message => message.Id == command.EvidenceMessageId.Value &&
                    message.ConversationId == conversation.Id,
                cancellationToken);
            if (!evidenceExists)
            {
                throw new DomainException("Evidence message is not part of this conversation.");
            }
        }

        bool openReportExists = await dbContext.Set<ConversationReport>()
            .AsNoTracking()
            .AnyAsync(report =>
                report.ConversationId == conversation.Id &&
                report.ReporterId == command.UserId &&
                (report.Status == ConversationReportStatus.Pending ||
                 report.Status == ConversationReportStatus.Reviewing),
                cancellationToken);
        if (openReportExists)
        {
            throw new DomainException("An open report already exists for this conversation.");
        }

        ConversationReport report = ConversationReport.Create(
            conversation.Id,
            command.UserId,
            (ConversationReportReason)command.Reason,
            command.Details,
            command.EvidenceMessageId,
            clock.UtcNow);
        dbContext.Set<ConversationReport>().Add(report);

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
                "An open report already exists for this conversation.");
        }

        return Map(report);
    }

    public async Task<IReadOnlyList<ConversationReportResult>?> GetMineAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        RequireUser(userId);
        bool canAccess = await dbContext.Conversations.AsNoTracking().AnyAsync(
            item => item.Id == conversationId &&
                (item.BuyerId == userId || item.SellerId == userId),
            cancellationToken);
        if (!canAccess) { return null; }

        return await dbContext.Set<ConversationReport>().AsNoTracking()
            .Where(report => report.ConversationId == conversationId &&
                report.ReporterId == userId)
            .OrderByDescending(report => report.CreatedAtUtc)
            .Select(report => new ConversationReportResult(
                report.Id, report.ConversationId, report.ReporterId,
                (int)report.Reason, report.Details, report.EvidenceMessageId,
                (int)report.Status, report.CreatedAtUtc,
                report.ReviewedByUserId, report.ReviewedAtUtc,
                report.ResolutionNote))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<PagedResult<ConversationReportSummaryResult>> GetForAdminAsync(
        int? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ValidatePage(page, pageSize);
        ConversationReportStatus? parsedStatus = ParseStatus(status);

        IQueryable<ConversationReport> query = dbContext.Set<ConversationReport>()
            .AsNoTracking();
        if (parsedStatus.HasValue)
        {
            query = query.Where(report => report.Status == parsedStatus.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(report => report.CreatedAtUtc)
            .ThenByDescending(report => report.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Join(dbContext.Conversations.AsNoTracking(),
                report => report.ConversationId,
                conversation => conversation.Id,
                (report, conversation) => new { report, conversation })
            .Join(dbContext.Listings.AsNoTracking(),
                row => row.conversation.ListingId,
                listing => listing.Id,
                (row, listing) => new ConversationReportSummaryResult(
                    row.report.Id,
                    row.report.ConversationId,
                    listing.Id,
                    listing.Title,
                    row.report.ReporterId,
                    row.conversation.BuyerId == row.report.ReporterId
                        ? row.conversation.SellerId
                        : row.conversation.BuyerId,
                    (int)row.report.Reason,
                    (int)row.report.Status,
                    row.report.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<ConversationReportSummaryResult>(
            rows, page, pageSize, totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    public async Task<ConversationReportDetailsResult?> GetDetailsAsync(
        Guid reportId,
        CancellationToken cancellationToken = default)
    {
        var header = await dbContext.Set<ConversationReport>().AsNoTracking()
            .Where(report => report.Id == reportId)
            .Join(dbContext.Conversations.AsNoTracking(),
                report => report.ConversationId,
                conversation => conversation.Id,
                (report, conversation) => new { report, conversation })
            .Join(dbContext.Listings.AsNoTracking(),
                row => row.conversation.ListingId,
                listing => listing.Id,
                (row, listing) => new
                {
                    Report = row.report,
                    Conversation = row.conversation,
                    ListingId = listing.Id,
                    ListingTitle = listing.Title
                })
            .SingleOrDefaultAsync(cancellationToken);
        if (header is null) { return null; }

        Message[] messages = await dbContext.Messages.AsNoTracking()
            .Include(message => message.Images)
            .Where(message => message.ConversationId == header.Conversation.Id)
            .OrderByDescending(message => message.SentAtUtc)
            .ThenByDescending(message => message.Id)
            .Take(100)
            .ToArrayAsync(cancellationToken);

        ConversationReportMessageResult[] evidence = messages
            .OrderBy(message => message.SentAtUtc)
            .ThenBy(message => message.Id)
            .Select(message => new ConversationReportMessageResult(
                message.Id,
                message.SenderId,
                message.Body,
                message.SentAtUtc,
                message.ReadAtUtc,
                message.Images
                    .OrderBy(image => image.DisplayOrder)
                    .Select(image => new MessageImageResult(
                        image.Id,
                        $"/api/admin/conversation-reports/{reportId:D}/images/{image.Id:D}",
                        image.ContentType,
                        image.SizeBytes,
                        image.DisplayOrder))
                    .ToArray()))
            .ToArray();

        return new ConversationReportDetailsResult(
            Map(header.Report),
            header.ListingId,
            header.ListingTitle,
            header.Conversation.BuyerId,
            header.Conversation.SellerId,
            evidence);
    }

    public async Task<ConversationReportResult?> ReviewAsync(
        Guid reviewerId,
        Guid reportId,
        int status,
        string? resolutionNote,
        CancellationToken cancellationToken = default)
    {
        RequireUser(reviewerId);
        if (!Enum.IsDefined(typeof(ConversationReportStatus), status))
        {
            throw new DomainException("Report status is invalid.");
        }

        ConversationReport? report = await dbContext.Set<ConversationReport>()
            .SingleOrDefaultAsync(item => item.Id == reportId, cancellationToken);
        if (report is null) { return null; }

        report.Review(
            (ConversationReportStatus)status,
            reviewerId,
            resolutionNote,
            clock.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        string notificationTitle =
            report.Status switch
            {
                ConversationReportStatus.Reviewing =>
                    "Şikayət araşdırılır",

                ConversationReportStatus.Resolved =>
                    "Şikayət həll edildi",

                ConversationReportStatus.Rejected =>
                    "Şikayət rədd edildi",

                _ =>
                    "Şikayət statusu yeniləndi"
            };

        string notificationBody =
            report.ResolutionNote ??
            "Göndərdiyiniz şikayət araşdırmaya götürüldü.";

        await notificationService.CreateAsync(
            new CreateNotificationCommand(
                report.ReporterId,
                (int)NotificationType
                    .ConversationReportUpdated,
                notificationTitle,
                notificationBody,
                report.ConversationId,
                $"/conversations/{report.ConversationId:D}"),
            cancellationToken);

        return Map(report);
    }

    public async Task<MessageImageDownload?> OpenEvidenceImageAsync(
        Guid reportId,
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        var row = await (
            from report in dbContext.Set<ConversationReport>().AsNoTracking()
            join message in dbContext.Messages.AsNoTracking()
                on report.ConversationId equals message.ConversationId
            join image in dbContext.Set<MessageImage>().AsNoTracking()
                on message.Id equals image.MessageId
            where report.Id == reportId && image.Id == imageId
            select new { image.StorageKey, image.ContentType })
            .SingleOrDefaultAsync(cancellationToken);
        if (row is null) { return null; }

        Stream? stream = await fileStorage.OpenReadAsync(row.StorageKey, cancellationToken);
        return stream is null ? null : new MessageImageDownload(stream, row.ContentType);
    }

    private static ConversationReportResult Map(ConversationReport report)
    {
        return new ConversationReportResult(
            report.Id, report.ConversationId, report.ReporterId,
            (int)report.Reason, report.Details, report.EvidenceMessageId,
            (int)report.Status, report.CreatedAtUtc,
            report.ReviewedByUserId, report.ReviewedAtUtc,
            report.ResolutionNote);
    }

    private static ConversationReportStatus? ParseStatus(int? status)
    {
        if (!status.HasValue) { return null; }
        if (!Enum.IsDefined(typeof(ConversationReportStatus), status.Value))
        {
            throw new DomainException("Report status is invalid.");
        }
        return (ConversationReportStatus)status.Value;
    }

    private static void RequireUser(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }
    }

    private static void ValidatePage(int page, int pageSize)
    {
        if (page < 1 || page > 1_000_000 || pageSize < 1 || pageSize > 100)
        {
            throw new DomainException("Page must be 1..1000000 and pageSize 1..100.");
        }
    }
}
