using RentoX.Application.Common;

namespace RentoX.Application.Moderation;

public sealed record ContentReportResult(
    Guid Id,
    int TargetType,
    Guid TargetId,
    int Reason,
    string? Details,
    int Status,
    DateTimeOffset CreatedAtUtc);

public sealed record ContentReportSummaryResult(
    Guid Id,
    int TargetType,
    Guid TargetId,
    string? TargetTitle,
    Guid ReporterId,
    int Reason,
    int Status,
    DateTimeOffset CreatedAtUtc);

public sealed record ContentReportDetailsResult(
    Guid Id,
    int TargetType,
    Guid TargetId,
    string? TargetTitle,
    Guid? TargetOwnerId,
    Guid ReporterId,
    int Reason,
    string? Details,
    int Status,
    DateTimeOffset CreatedAtUtc,
    Guid? ReviewedByUserId,
    DateTimeOffset? ReviewedAtUtc,
    string? ResolutionNote,
    int OpenReportsOnTarget);

public interface IContentReportService
{
    /// <summary>
    /// Reports a listing or store. Returns <c>null</c> when the target does
    /// not exist or is not public.
    /// </summary>
    Task<ContentReportResult?> CreateAsync(
        Guid reporterId,
        int targetType,
        Guid targetId,
        int reason,
        string? details,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ContentReportSummaryResult>> GetForAdminAsync(
        int? status,
        int? targetType,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ContentReportDetailsResult?> GetDetailsAsync(
        Guid reportId,
        CancellationToken cancellationToken = default);

    Task<ContentReportResult?> ReviewAsync(
        Guid reviewerId,
        Guid reportId,
        int status,
        string? resolutionNote,
        CancellationToken cancellationToken = default);
}
