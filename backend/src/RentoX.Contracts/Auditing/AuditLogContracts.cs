namespace RentoX.Contracts.Auditing;

public sealed class SearchAuditLogsRequest
{
    public Guid? ActorUserId { get; set; }

    public Guid? TargetId { get; set; }

    public Guid? OperationId { get; set; }

    public int? Action { get; set; }

    public DateTimeOffset? FromUtc { get; set; }

    public DateTimeOffset? ToUtc { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}

public sealed record AuditLogItemResponse(
    Guid Id,
    Guid OperationId,
    Guid ActorUserId,
    Guid TargetId,
    int Action,
    int? PreviousValue,
    int? CurrentValue,
    Guid? RelatedEntityId,
    DateTimeOffset OccurredAtUtc);

public sealed record AuditLogPageResponse(
    IReadOnlyList<AuditLogItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc);