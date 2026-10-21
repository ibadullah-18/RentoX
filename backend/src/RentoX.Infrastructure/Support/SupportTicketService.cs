using System.Data;
using RentoX.Domain.Auditing;
using RentoX.Application.Abstractions.Authentication;
using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Common;
using RentoX.Application.Notifications;
using RentoX.Application.Support;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Notifications;
using RentoX.Domain.Support;
using RentoX.Domain.Users;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Support;

public sealed class SupportTicketService(
    RentoXDbContext dbContext,
    IClock clock,
    INotificationService notificationService,
    ICurrentUserContext currentUserContext,
    ISupportEventPublisher? live = null,
    RentoX.Application.Files.IFileStorage? fileStorage = null)
    : ISupportTicketService
{
    public async Task<SupportTicketDetailsResult> CreateAsync(
        CreateSupportTicketCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        RequireUser(command.UserId);

        if (!Enum.IsDefined(
                typeof(SupportTicketCategory),
                command.Category))
        {
            throw new DomainException(
                "Support ticket category is invalid.");
        }

        SupportTicket ticket = SupportTicket.Create(
            command.UserId,
            (SupportTicketCategory)command.Category,
            command.Subject,
            command.InitialMessage,
            clock.UtcNow);

        dbContext.Set<SupportTicket>().Add(ticket);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (live is not null)
        {
            string? ownerName = await NameOfAsync(ticket.UserId, cancellationToken);
            await live.TicketCreatedAsync(
                ticket.Id, ticket.UserId, ticket.Subject, ownerName, cancellationToken);
        }

        return MapDetails(ticket);
    }

    public async Task<PagedResult<SupportTicketSummaryResult>> GetMineAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        RequireUser(userId);

        IQueryable<SupportTicket> query =
            dbContext.Set<SupportTicket>()
                .AsNoTracking()
                .Where(ticket => ticket.UserId == userId);

        return await CreatePageAsync(
            query,
            page,
            pageSize,
            cancellationToken);
    }

    public async Task<SupportTicketDetailsResult?> GetMineDetailsAsync(
        Guid userId,
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        RequireUser(userId);

        SupportTicket? ticket =
            await dbContext.Set<SupportTicket>()
                .AsNoTracking()
                .Include(item => item.Messages)
                    .ThenInclude(message => message.Images)
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == ticketId &&
                        item.UserId == userId,
                    cancellationToken);

        return ticket is null
            ? null
            : MapDetails(ticket);
    }

    public Task<SupportTicketMessageResult?> AddUserMessageAsync(
        Guid userId,
        Guid ticketId,
        string body,
        CancellationToken cancellationToken = default) =>
        AddUserMessageWithImagesAsync(userId, ticketId, body, null, cancellationToken);

    public async Task<SupportTicketMessageResult?> AddUserMessageWithImagesAsync(
        Guid userId,
        Guid ticketId,
        string body,
        IReadOnlyList<SupportImageUpload>? images,
        CancellationToken cancellationToken = default)
    {
        RequireUser(userId);
        SupportImageStore.Validate(images, body);

        SupportTicket? ticket =
            await dbContext.Set<SupportTicket>()
                .Include(item => item.Messages)
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == ticketId &&
                        item.UserId == userId,
                    cancellationToken);

        if (ticket is null)
        {
            return null;
        }

        List<SupportTicketImageFile> files = await StoreImagesAsync(images, cancellationToken);
        SupportTicketMessage message;
        try
        {
            ticket.AddUserMessage(
                userId,
                body,
                clock.UtcNow,
                files);

            message = ticket.Messages.Last();

            dbContext.Set<SupportTicketMessage>().Add(message);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await DiscardImagesAsync(files);
            throw;
        }

        SupportTicketMessageResult result = await WithSenderNameAsync(message, cancellationToken);
        if (live is not null)
        {
            await live.MessageAddedAsync(ticket.Id, ticket.UserId, result, cancellationToken);
        }

        return result;
    }

    public async Task<PagedResult<SupportTicketSummaryResult>> GetForAdminAsync(
        int? status,
        int? priority,
        int? category,
        int page,
        int pageSize,
        bool openOnly = false,
        CancellationToken cancellationToken = default)
    {
        ValidateOptionalEnum<SupportTicketStatus>(
            status,
            "Support ticket status is invalid.");

        ValidateOptionalEnum<SupportTicketPriority>(
            priority,
            "Support ticket priority is invalid.");

        ValidateOptionalEnum<SupportTicketCategory>(
            category,
            "Support ticket category is invalid.");

        IQueryable<SupportTicket> query =
            dbContext.Set<SupportTicket>()
                .AsNoTracking();

        if (status.HasValue)
        {
            SupportTicketStatus value =
                (SupportTicketStatus)status.Value;

            query = query.Where(ticket =>
                ticket.Status == value);
        }

        if (priority.HasValue)
        {
            SupportTicketPriority value =
                (SupportTicketPriority)priority.Value;

            query = query.Where(ticket =>
                ticket.Priority == value);
        }

        if (category.HasValue)
        {
            SupportTicketCategory value =
                (SupportTicketCategory)category.Value;

            query = query.Where(ticket =>
                ticket.Category == value);
        }

        if (openOnly)
        {
            query = query.Where(ticket =>
                ticket.Status == SupportTicketStatus.Open ||
                ticket.Status == SupportTicketStatus.InProgress);
        }

        ValidatePagination(page, pageSize);

        int totalCount = await query.CountAsync(cancellationToken);

        var rows = query.Select(ticket => new
        {
            Ticket = ticket,
            LastIsAdmin = ticket.Messages
                .OrderByDescending(message => message.SentAtUtc)
                .Select(message => (bool?)message.IsAdmin)
                .FirstOrDefault(),
            LastAt = ticket.Messages
                .OrderByDescending(message => message.SentAtUtc)
                .Select(message => (DateTimeOffset?)message.SentAtUtc)
                .FirstOrDefault(),
            LastBody = ticket.Messages
                .OrderByDescending(message => message.SentAtUtc)
                .Select(message => message.Body)
                .FirstOrDefault(),
            Count = ticket.Messages.Count
        });

        // The queue is worked like this: open ones first; of those, the ones waiting for an answer from us first
        // (the last word is the user's); then the most urgent; then the longest waiting. A closed list is read from the newest.
        bool closed = status is (int)SupportTicketStatus.Resolved or (int)SupportTicketStatus.Closed;

        var ordered = closed
            ? rows.OrderByDescending(row => row.Ticket.UpdatedAtUtc)
            : rows
                .OrderBy(row => row.Ticket.Status == SupportTicketStatus.Resolved ||
                                row.Ticket.Status == SupportTicketStatus.Closed ? 1 : 0)
                .ThenBy(row => row.LastIsAdmin == true ? 1 : 0)
                .ThenByDescending(row => row.Ticket.Priority)
                .ThenBy(row => row.Ticket.UpdatedAtUtc);

        var page_ = await ordered
            .ThenBy(row => row.Ticket.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        Guid[] userIds = page_.Select(row => row.Ticket.UserId).Distinct().ToArray();
        Dictionary<Guid, string> names = await NamesAsync(userIds, cancellationToken);

        SupportTicketSummaryResult[] items = page_
            .Select(row => new SupportTicketSummaryResult(
                row.Ticket.Id,
                row.Ticket.UserId,
                (int)row.Ticket.Category,
                row.Ticket.Subject,
                (int)row.Ticket.Priority,
                (int)row.Ticket.Status,
                row.Ticket.CreatedAtUtc,
                row.Ticket.UpdatedAtUtc,
                names.GetValueOrDefault(row.Ticket.UserId),
                row.LastAt,
                row.LastIsAdmin,
                Preview(row.LastBody),
                row.Count))
            .ToArray();

        return new PagedResult<SupportTicketSummaryResult>(
            items,
            page,
            pageSize,
            totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    public async Task<Guid?> GetOwnerIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<SupportTicket>()
            .AsNoTracking()
            .Where(ticket => ticket.Id == ticketId)
            .Select(ticket => (Guid?)ticket.UserId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SupportTicketDetailsResult?> GetAdminDetailsAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        SupportTicket? ticket =
            await dbContext.Set<SupportTicket>()
                .AsNoTracking()
                .Include(item => item.Messages)
                    .ThenInclude(message => message.Images)
                .SingleOrDefaultAsync(
                    item => item.Id == ticketId,
                    cancellationToken);

        if (ticket is null)
        {
            return null;
        }

        // Names of everybody who wrote, the person's phone, and how many other requests they have.
        Guid[] senderIds = ticket.Messages.Select(message => message.SenderId)
            .Append(ticket.UserId)
            .Distinct()
            .ToArray();
        Dictionary<Guid, string> names = await NamesAsync(senderIds, cancellationToken);
        string? phone = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == ticket.UserId)
            .Select(user => user.PhoneNumber)
            .SingleOrDefaultAsync(cancellationToken);
        int others = await dbContext.Set<SupportTicket>()
            .AsNoTracking()
            .CountAsync(
                other => other.UserId == ticket.UserId && other.Id != ticket.Id,
                cancellationToken);

        // What the person was told about this request, and whether it reached them.
        var sent = await dbContext.Set<RentoX.Domain.Notifications.Notification>()
            .AsNoTracking()
            .Where(n => n.RelatedEntityId == ticket.Id &&
                        n.UserId == ticket.UserId &&
                        (n.Type == NotificationType.SupportReply ||
                         n.Type == NotificationType.SupportStatusChanged))
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(30)
            .Select(n => new
            {
                n.Id,
                Type = (int)n.Type,
                n.Title,
                n.Body,
                n.CreatedAtUtc,
                n.ReadAtUtc
            })
            .ToListAsync(cancellationToken);

        Guid[] sentIds = sent.Select(n => n.Id).ToArray();
        var pushes = await dbContext.Set<RentoX.Domain.Notifications.PushNotificationDelivery>()
            .AsNoTracking()
            .Where(p => sentIds.Contains(p.NotificationId))
            .Select(p => new { p.NotificationId, Status = (int)p.Status, p.AttemptCount, p.SentAtUtc, p.LastError })
            .ToListAsync(cancellationToken);
        int devices = await dbContext.Set<RentoX.Domain.Notifications.PushDevice>()
            .AsNoTracking()
            .CountAsync(d => d.UserId == ticket.UserId && d.IsActive, cancellationToken);

        SupportTicketDetailsResult details = MapDetails(ticket);
        return details with
        {
            Notifications = sent
                .Select(n => new SupportNotificationLog(
                    n.Type,
                    n.Title,
                    n.CreatedAtUtc,
                    n.ReadAtUtc,
                    pushes.Where(p => p.NotificationId == n.Id).Select(p => (int?)p.Status).Max(),
                    n.Body,
                    pushes.Where(p => p.NotificationId == n.Id).Select(p => p.AttemptCount).DefaultIfEmpty(0).Max(),
                    pushes.Where(p => p.NotificationId == n.Id).Select(p => p.SentAtUtc).FirstOrDefault(),
                    pushes.Where(p => p.NotificationId == n.Id).Select(p => p.LastError).FirstOrDefault()))
                .ToArray(),
            UserDevices = devices,
            Messages = details.Messages
                .Select(message => message with { SenderName = names.GetValueOrDefault(message.SenderId) })
                .ToArray(),
            UserName = names.GetValueOrDefault(ticket.UserId),
            UserPhone = phone,
            OtherTickets = others
        };
    }

    public Task<SupportTicketMessageResult?> AddAdminMessageAsync(
        Guid adminUserId,
        Guid ticketId,
        string body,
        CancellationToken cancellationToken = default) =>
        AddAdminMessageWithImagesAsync(adminUserId, ticketId, body, null, cancellationToken);

    public async Task<SupportTicketMessageResult?> AddAdminMessageWithImagesAsync(
        Guid adminUserId,
        Guid ticketId,
        string body,
        IReadOnlyList<SupportImageUpload>? images,
        CancellationToken cancellationToken = default)
    {
        Guid actorUserId = RequireAuditActor();
        SupportImageStore.Validate(images, body);

        if (adminUserId != actorUserId)
        {
            throw new DomainException(
                "Support administrator does not match the authenticated user.");
        }

        Guid operationId = Guid.NewGuid();

        await using var auditTransaction =
            await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        RequireUser(adminUserId);

        SupportTicket? ticket =
            await LoadTicketAsync(
                ticketId,
                cancellationToken);

        if (ticket is null)
        {
            return null;
        }

        SupportTicketStatus previousStatus = ticket.Status;

        List<SupportTicketImageFile> files = await StoreImagesAsync(images, cancellationToken);
        SupportTicketMessage message;
        try
        {
            ticket.AddAdminMessage(
                actorUserId,
                body,
                clock.UtcNow,
                files);

            message = ticket.Messages.Last();

            dbContext.Set<SupportTicketMessage>().Add(message);

            dbContext.Set<AuditLogEntry>().Add(
                AuditLogEntry.ForSupportReply(
                    operationId,
                    actorUserId,
                    ticket.Id,
                    message.Id,
                    clock.UtcNow));

            RecordSupportStatusChange(
                ticket, previousStatus, actorUserId, operationId);

            await dbContext.SaveChangesAsync(cancellationToken);
            await auditTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await DiscardImagesAsync(files);
            throw;
        }

        await notificationService.CreateAsync(
            new CreateNotificationCommand(
                ticket.UserId,
                (int)NotificationType.SupportReply,
                "Dəstək müraciətinizə cavab verildi",
                $"“{ticket.Subject}” müraciətinə yeni cavab əlavə edildi.",
                ticket.Id,
                $"/support/tickets/{ticket.Id:D}"),
            cancellationToken);

        SupportTicketMessageResult result = await WithSenderNameAsync(message, cancellationToken);
        if (live is not null)
        {
            await live.MessageAddedAsync(ticket.Id, ticket.UserId, result, cancellationToken);
            if (ticket.Status != previousStatus)
            {
                await live.TicketChangedAsync(
                    ticket.Id, ticket.UserId, (int)ticket.Status, (int)ticket.Priority, cancellationToken);
            }
        }

        return result;
    }

    public async Task<SupportTicketDetailsResult?> ChangeStatusAsync(
        Guid adminUserId,
        Guid ticketId,
        int status,
        CancellationToken cancellationToken = default)
    {
        Guid actorUserId = RequireAuditActor();

        if (adminUserId != actorUserId)
        {
            throw new DomainException(
                "Support administrator does not match the authenticated user.");
        }

        Guid operationId = Guid.NewGuid();

        await using var auditTransaction =
            await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        RequireUser(adminUserId);

        if (!Enum.IsDefined(
                typeof(SupportTicketStatus),
                status))
        {
            throw new DomainException(
                "Support ticket status is invalid.");
        }

        SupportTicket? ticket =
            await LoadTicketAsync(
                ticketId,
                cancellationToken);

        if (ticket is null)
        {
            return null;
        }

        SupportTicketStatus newStatus =
            (SupportTicketStatus)status;

        if (ticket.Status != newStatus)
        {
            SupportTicketStatus previousStatus = ticket.Status;

            ticket.ChangeStatus(
                newStatus,
                clock.UtcNow);

            RecordSupportStatusChange(
                ticket, previousStatus, actorUserId, operationId);

            await dbContext.SaveChangesAsync(cancellationToken);
            await auditTransaction.CommitAsync(cancellationToken);

            await notificationService.CreateAsync(
                new CreateNotificationCommand(
                    ticket.UserId,
                    (int)NotificationType.SupportStatusChanged,
                    "Dəstək müraciətinin statusu dəyişdi",
                    $"“{ticket.Subject}” müraciətinin yeni statusu: {GetStatusName(newStatus)}.",
                    ticket.Id,
                    $"/support/tickets/{ticket.Id:D}"),
                cancellationToken);

            if (live is not null)
            {
                await live.TicketChangedAsync(
                    ticket.Id, ticket.UserId, (int)ticket.Status, (int)ticket.Priority, cancellationToken);
            }
        }

        return MapDetails(ticket);
    }

    public async Task<SupportTicketDetailsResult?> ChangePriorityAsync(
        Guid adminUserId,
        Guid ticketId,
        int priority,
        CancellationToken cancellationToken = default)
    {
        Guid actorUserId = RequireAuditActor();

        if (adminUserId != actorUserId)
        {
            throw new DomainException(
                "Support administrator does not match the authenticated user.");
        }

        Guid operationId = Guid.NewGuid();

        await using var auditTransaction =
            await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        RequireUser(adminUserId);

        if (!Enum.IsDefined(
                typeof(SupportTicketPriority),
                priority))
        {
            throw new DomainException(
                "Support ticket priority is invalid.");
        }

        SupportTicket? ticket =
            await LoadTicketAsync(
                ticketId,
                cancellationToken);

        if (ticket is null)
        {
            return null;
        }

        SupportTicketPriority newPriority =
            (SupportTicketPriority)priority;

        if (ticket.Priority == newPriority)
        {
            return MapDetails(ticket);
        }

        SupportTicketPriority previousPriority = ticket.Priority;

        ticket.ChangePriority(
            newPriority,
            clock.UtcNow);

        dbContext.Set<AuditLogEntry>().Add(
            AuditLogEntry.ForSupportPriorityChange(
                operationId,
                actorUserId,
                ticket.Id,
                previousPriority,
                ticket.Priority,
                clock.UtcNow));

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await auditTransaction.CommitAsync(cancellationToken);

        if (live is not null)
        {
            await live.TicketChangedAsync(
                ticket.Id, ticket.UserId, (int)ticket.Status, (int)ticket.Priority, cancellationToken);
        }

        return MapDetails(ticket);
    }

    private Guid RequireAuditActor()
    {
        Guid? userId = currentUserContext.UserId;

        if (!currentUserContext.IsAuthenticated ||
            !userId.HasValue ||
            userId.Value == Guid.Empty)
        {
            throw new DomainException(
                "An authenticated audit actor is required.");
        }

        return userId.Value;
    }

    private void RecordSupportStatusChange(
        SupportTicket ticket,
        SupportTicketStatus previousStatus,
        Guid actorUserId,
        Guid operationId)
    {
        if (previousStatus == ticket.Status)
        {
            return;
        }

        dbContext.Set<AuditLogEntry>().Add(
            AuditLogEntry.ForSupportStatusChange(
                operationId,
                actorUserId,
                ticket.Id,
                previousStatus,
                ticket.Status,
                clock.UtcNow));
    }

    private async Task<SupportTicket?> LoadTicketAsync(
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<SupportTicket>()
            .Include(ticket => ticket.Messages)
                .ThenInclude(message => message.Images)
            .SingleOrDefaultAsync(
                ticket => ticket.Id == ticketId,
                cancellationToken);
    }

    private static async Task<PagedResult<SupportTicketSummaryResult>>
        CreatePageAsync(
            IQueryable<SupportTicket> query,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
    {
        ValidatePagination(page, pageSize);

        int totalCount =
            await query.CountAsync(cancellationToken);

        SupportTicketSummaryResult[] items =
            await query
                .OrderByDescending(ticket =>
                    ticket.UpdatedAtUtc)
                .ThenByDescending(ticket =>
                    ticket.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ticket =>
                    new SupportTicketSummaryResult(
                        ticket.Id,
                        ticket.UserId,
                        (int)ticket.Category,
                        ticket.Subject,
                        (int)ticket.Priority,
                        (int)ticket.Status,
                        ticket.CreatedAtUtc,
                        ticket.UpdatedAtUtc))
                .ToArrayAsync(cancellationToken);

        int totalPages =
            totalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    totalCount / (double)pageSize);

        return new PagedResult<SupportTicketSummaryResult>(
            items,
            page,
            pageSize,
            totalCount,
            totalPages);
    }

    private async Task<string?> NameOfAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<UserProfile>()
            .AsNoTracking()
            .Where(profile => profile.Id == userId)
            .Select(profile => profile.FullName)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<SupportTicketMessageResult> WithSenderNameAsync(
        SupportTicketMessage message, CancellationToken cancellationToken)
    {
        return MapMessage(message) with { SenderName = await NameOfAsync(message.SenderId, cancellationToken) };
    }

    private async Task<Dictionary<Guid, string>> NamesAsync(Guid[] userIds, CancellationToken cancellationToken)
    {
        return await dbContext.Set<UserProfile>()
            .AsNoTracking()
            .Where(profile => userIds.Contains(profile.Id))
            .ToDictionaryAsync(profile => profile.Id, profile => profile.FullName, cancellationToken);
    }

    private static string? Preview(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        string flat = string.Join(' ', body.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return flat.Length <= 140 ? flat : flat[..140] + "…";
    }

    private static SupportTicketDetailsResult MapDetails(
        SupportTicket ticket)
    {
        return new SupportTicketDetailsResult(
            ticket.Id,
            ticket.UserId,
            (int)ticket.Category,
            ticket.Subject,
            (int)ticket.Priority,
            (int)ticket.Status,
            ticket.CreatedAtUtc,
            ticket.UpdatedAtUtc,
            ticket.ResolvedAtUtc,
            ticket.ClosedAtUtc,
            ticket.Messages
                .OrderBy(message =>
                    message.SentAtUtc)
                .ThenBy(message =>
                    message.Id)
                .Select(MapMessage)
                .ToArray());
    }

    private static SupportTicketMessageResult MapMessage(
        SupportTicketMessage message)
    {
        return new SupportTicketMessageResult(
            message.Id,
            message.SenderId,
            message.IsAdmin,
            message.Body,
            message.SentAtUtc,
            null,
            message.Images
                .OrderBy(image => image.DisplayOrder)
                .Select(image => new SupportImageResult(image.Id, image.ContentType, image.SizeBytes, image.DisplayOrder))
                .ToArray());
    }

    public async Task<SupportImageDownload?> OpenImageAsync(
        Guid ticketId,
        Guid imageId,
        Guid? ownerId,
        CancellationToken cancellationToken = default)
    {
        if (fileStorage is null)
        {
            return null;
        }

        var image = await (
            from attachment in dbContext.Set<SupportTicketImage>().AsNoTracking()
            join message in dbContext.Set<SupportTicketMessage>().AsNoTracking() on attachment.MessageId equals message.Id
            join ticket in dbContext.Set<SupportTicket>().AsNoTracking() on message.SupportTicketId equals ticket.Id
            where attachment.Id == imageId && ticket.Id == ticketId && (ownerId == null || ticket.UserId == ownerId)
            select new { attachment.StorageKey, attachment.ContentType })
            .SingleOrDefaultAsync(cancellationToken);
        if (image is null)
        {
            return null;
        }

        Stream? content = await fileStorage.OpenReadAsync(image.StorageKey, cancellationToken);
        return content is null ? null : new SupportImageDownload(content, image.ContentType);
    }

    private async Task<List<SupportTicketImageFile>> StoreImagesAsync(
        IReadOnlyList<SupportImageUpload>? images, CancellationToken cancellationToken)
    {
        if (images is null || images.Count == 0)
        {
            return [];
        }

        if (fileStorage is null)
        {
            throw new DomainException("Photos cannot be saved now.");
        }

        return await SupportImageStore.StoreAsync(fileStorage, images, cancellationToken);
    }

    private async Task DiscardImagesAsync(List<SupportTicketImageFile> files)
    {
        if (fileStorage is not null && files.Count > 0)
        {
            await SupportImageStore.DiscardAsync(fileStorage, files, null);
        }
    }

    private static string GetStatusName(
        SupportTicketStatus status)
    {
        return status switch
        {
            SupportTicketStatus.Open => "Açıq",
            SupportTicketStatus.InProgress => "İşlənir",
            SupportTicketStatus.Resolved => "Həll edildi",
            SupportTicketStatus.Closed => "Bağlandı",
            _ => throw new DomainException(
                "Support ticket status is invalid.")
        };
    }

    private static void RequireUser(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException(
                "Support user id is required.");
        }
    }

    private static void ValidateOptionalEnum<TEnum>(
        int? value,
        string error)
        where TEnum : struct, Enum
    {
        if (value.HasValue &&
            !Enum.IsDefined(
                typeof(TEnum),
                value.Value))
        {
            throw new DomainException(error);
        }
    }

    private static void ValidatePagination(
        int page,
        int pageSize)
    {
        if (page < 1 ||
            pageSize < 1 ||
            pageSize > 100)
        {
            throw new DomainException(
                "Page must be positive and page size must be between 1 and 100.");
        }
    }
}