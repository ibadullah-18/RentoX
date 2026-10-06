namespace RentoX.Application.Auditing;

public interface IAuditLogQueryService
{
    Task<AuditLogPageResult> SearchAsync(
        AuditLogSearchQuery query,
        CancellationToken cancellationToken = default);
}