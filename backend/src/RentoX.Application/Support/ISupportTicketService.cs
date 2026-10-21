using RentoX.Application.Common;

namespace RentoX.Application.Support;

public sealed record CreateSupportTicketCommand(
    Guid UserId,
    int Category,
    string Subject,
    string InitialMessage);

/// <summary>A photo inside a message; it is fetched by its id from the request it belongs to.</summary>
public sealed record SupportImageResult(Guid Id, string ContentType, long SizeBytes, int DisplayOrder);

public sealed record SupportTicketMessageResult(
    Guid Id,
    Guid SenderId,
    bool IsAdmin,
    string Body,
    DateTimeOffset SentAtUtc,
    string? SenderName = null,
    IReadOnlyList<SupportImageResult>? Images = null);

// The caller owns the upload streams; the HTTP response owns a returned download stream.
public sealed record SupportImageUpload(Stream Content, string ContentType, long SizeBytes);

public sealed record SupportImageDownload(Stream Content, string ContentType);

public sealed record SupportTicketSummaryResult(
    Guid Id,
    Guid UserId,
    int Category,
    string Subject,
    int Priority,
    int Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string? UserName = null,
    DateTimeOffset? LastMessageAtUtc = null,
    bool? LastMessageIsAdmin = null,
    string? LastMessagePreview = null,
    int MessageCount = 0);

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
    IReadOnlyList<SupportTicketMessageResult> Messages,
    string? UserName = null,
    string? UserPhone = null,
    int OtherTickets = 0,
    IReadOnlyList<SupportNotificationLog>? Notifications = null,
    int UserDevices = 0);

/// <summary>
/// One notification the person was sent about this request (a reply or a change of state): when, whether it was
/// read, and how the push to the phone went (null: nothing was queued, 1 waiting, 2 sending, 3 sent, 4 failed, 5 gave up).
/// </summary>
public sealed record SupportNotificationLog(
    int Type,
    string Title,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReadAtUtc,
    int? PushStatus,
    string? Body = null,
    int PushAttempts = 0,
    DateTimeOffset? PushSentAtUtc = null,
    string? PushError = null);

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

    /// <param name="images">Up to 5 photos sent with (or instead of) the text.</param>
    Task<SupportTicketMessageResult?> AddUserMessageWithImagesAsync(
        Guid userId,
        Guid ticketId,
        string body,
        IReadOnlyList<SupportImageUpload>? images,
        CancellationToken cancellationToken = default);

    /// <summary>One photo of a request. [ownerId]: the person (they see only their own); null for the staff.</summary>
    Task<SupportImageDownload?> OpenImageAsync(
        Guid ticketId,
        Guid imageId,
        Guid? ownerId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<SupportTicketSummaryResult>> GetForAdminAsync(
        int? status,
        int? priority,
        int? category,
        int page,
        int pageSize,
        bool openOnly = false,
        CancellationToken cancellationToken = default);

    /// <summary>Who owns the request, or null when there is none (for the live line).</summary>
    Task<Guid?> GetOwnerIdAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default);

    Task<SupportTicketDetailsResult?> GetAdminDetailsAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default);

    Task<SupportTicketMessageResult?> AddAdminMessageAsync(
        Guid adminUserId,
        Guid ticketId,
        string body,
        CancellationToken cancellationToken = default);

    Task<SupportTicketMessageResult?> AddAdminMessageWithImagesAsync(
        Guid adminUserId,
        Guid ticketId,
        string body,
        IReadOnlyList<SupportImageUpload>? images,
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