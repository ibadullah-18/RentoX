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
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Support;

public sealed class SupportTicketService(
    RentoXDbContext dbContext,
    IClock clock,
    INotificationService notificationService,
    ICurrentUserContext currentUserContext)
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
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == ticketId &&
                        item.UserId == userId,
                    cancellationToken);

        return ticket is null
            ? null
            : MapDetails(ticket);
    }

    public async Task<SupportTicketMessageResult?> AddUserMessageAsync(
        Guid userId,
        Guid ticketId,
        string body,
        CancellationToken cancellationToken = default)
    {
        RequireUser(userId);

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

        ticket.AddUserMessage(
            userId,
            body,
            clock.UtcNow);

        SupportTicketMessage message =
            ticket.Messages.Last();

        dbContext.Set<SupportTicketMessage>().Add(message);

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapMessage(message);
    }

    public async Task<PagedResult<SupportTicketSummaryResult>> GetForAdminAsync(
        int? status,
        int? priority,
        int? category,
        int page,
        int pageSize,
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

        return await CreatePageAsync(
            query,
            page,
            pageSize,
            cancellationToken);
    }

    public async Task<SupportTicketDetailsResult?> GetAdminDetailsAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        SupportTicket? ticket =
            await dbContext.Set<SupportTicket>()
                .AsNoTracking()
                .Include(item => item.Messages)
                .SingleOrDefaultAsync(
                    item => item.Id == ticketId,
                    cancellationToken);

        return ticket is null
            ? null
            : MapDetails(ticket);
    }

    public async Task<SupportTicketMessageResult?> AddAdminMessageAsync(
        Guid adminUserId,
        Guid ticketId,
        string body,
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

        SupportTicket? ticket =
            await LoadTicketAsync(
                ticketId,
                cancellationToken);

        if (ticket is null)
        {
            return null;
        }

        SupportTicketStatus previousStatus = ticket.Status;

        ticket.AddAdminMessage(
            actorUserId,
            body,
            clock.UtcNow);

        SupportTicketMessage message =
            ticket.Messages.Last();

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

        await notificationService.CreateAsync(
            new CreateNotificationCommand(
                ticket.UserId,
                (int)NotificationType.SupportReply,
                "Dəstək müraciətinizə cavab verildi",
                $"“{ticket.Subject}” müraciətinə yeni cavab əlavə edildi.",
                ticket.Id,
                $"/support/tickets/{ticket.Id:D}"),
            cancellationToken);

        return MapMessage(message);
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
            message.SentAtUtc);
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