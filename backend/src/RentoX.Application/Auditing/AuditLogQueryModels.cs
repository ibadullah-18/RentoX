namespace RentoX.Application.Auditing;

public sealed record AuditLogSearchQuery(
    Guid? ActorUserId,
    Guid? TargetId,
    Guid? OperationId,
    int? Action,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    int Page,
    int PageSize);

public sealed record AuditLogItemResult(
    Guid Id,
    Guid OperationId,
    Guid ActorUserId,
    Guid TargetId,
    int Action,
    int? PreviousValue,
    int? CurrentValue,
    Guid? RelatedEntityId,
    DateTimeOffset OccurredAtUtc);

public sealed record AuditLogPageResult(
    IReadOnlyList<AuditLogItemResult> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc);