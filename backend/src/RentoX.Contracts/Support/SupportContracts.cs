namespace RentoX.Contracts.Support;

public sealed record CreateSupportTicketRequest(
    int Category,
    string Subject,
    string InitialMessage);

public sealed record AddSupportTicketMessageRequest(
    string Body);

public sealed record ChangeSupportTicketStatusRequest(
    int Status);

public sealed record ChangeSupportTicketPriorityRequest(
    int Priority);

public sealed record SupportTicketMessageResponse(
    Guid Id,
    Guid SenderId,
    bool IsAdmin,
    string Body,
    DateTimeOffset SentAtUtc,
    string? SenderName = null);

public sealed record SupportTicketSummaryResponse(
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

public sealed record SupportTicketDetailsResponse(
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
    IReadOnlyList<SupportTicketMessageResponse> Messages,
    string? UserName = null,
    string? UserPhone = null,
    int OtherTickets = 0,
    IReadOnlyList<SupportNotificationLogResponse>? Notifications = null,
    int UserDevices = 0);

/// <summary>Type 7 reply, 8 change of state. PushStatus: null none queued, 1 waiting, 2 sending, 3 sent, 4 failed, 5 gave up.</summary>
public sealed record SupportNotificationLogResponse(
    int Type,
    string Title,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReadAtUtc,
    int? PushStatus,
    string? Body = null,
    int PushAttempts = 0,
    DateTimeOffset? PushSentAtUtc = null,
    string? PushError = null);