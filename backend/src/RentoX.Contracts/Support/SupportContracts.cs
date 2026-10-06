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
    DateTimeOffset SentAtUtc);

public sealed record SupportTicketSummaryResponse(
    Guid Id,
    Guid UserId,
    int Category,
    string Subject,
    int Priority,
    int Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

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
    IReadOnlyList<SupportTicketMessageResponse> Messages);