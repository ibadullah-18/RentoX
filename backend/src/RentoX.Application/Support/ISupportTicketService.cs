using RentoX.Application.Common;

namespace RentoX.Application.Support;

public sealed record CreateSupportTicketCommand(
    Guid UserId,
    int Category,
    string Subject,
    string InitialMessage);

public sealed record SupportTicketMessageResult(
    Guid Id,
    Guid SenderId,
    bool IsAdmin,
    string Body,
    DateTimeOffset SentAtUtc);

public sealed record SupportTicketSummaryResult(
    Guid Id,
    Guid UserId,
    int Category,
    string Subject,
    int Priority,
    int Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record SupportTicketDetailsResult(
    Guid Id,
    Guid UserId,
    int Category,
    string Subject,
    int Priority,
    int Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? ResolvedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    IReadOnlyList<SupportTicketMessageResult> Messages);

public interface ISupportTicketService
{
    Task<SupportTicketDetailsResult> CreateAsync(
        CreateSupportTicketCommand command,
        CancellationToken cancellationToken = default);

    Task<PagedResult<SupportTicketSummaryResult>> GetMineAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<SupportTicketDetailsResult?> GetMineDetailsAsync(
        Guid userId,
        Guid ticketId,
        CancellationToken cancellationToken = default);

    Task<SupportTicketMessageResult?> AddUserMessageAsync(
        Guid userId,
        Guid ticketId,
        string body,
        CancellationToken cancellationToken = default);

    Task<PagedResult<SupportTicketSummaryResult>> GetForAdminAsync(
        int? status,
        int? priority,
        int? category,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<SupportTicketDetailsResult?> GetAdminDetailsAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default);

    Task<SupportTicketMessageResult?> AddAdminMessageAsync(
        Guid adminUserId,
        Guid ticketId,
        string body,
        CancellationToken cancellationToken = default);

    Task<SupportTicketDetailsResult?> ChangeStatusAsync(
        Guid adminUserId,
        Guid ticketId,
        int status,
        CancellationToken cancellationToken = default);

    Task<SupportTicketDetailsResult?> ChangePriorityAsync(
        Guid adminUserId,
        Guid ticketId,
        int priority,
        CancellationToken cancellationToken = default);
}