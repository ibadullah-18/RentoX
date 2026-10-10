namespace RentoX.Contracts.Moderation;

/// <summary>
/// Reasons: 1 spam, 2 fraud, 3 prohibited content, 4 misleading,
/// 5 wrong category, 6 other (details required).
/// </summary>
public sealed record CreateContentReportRequest(
    int Reason,
    string? Details);

/// <summary>TargetType: 1 listing, 2 store. Status: 1 pending, 2 reviewing, 3 resolved, 4 rejected.</summary>
public sealed record ContentReportResponse(
    Guid Id,
    int TargetType,
    Guid TargetId,
    int Reason,
    string? Details,
    int Status,
    DateTimeOffset CreatedAtUtc);

public sealed record ContentReportSummaryResponse(
    Guid Id,
    int TargetType,
    Guid TargetId,
    string? TargetTitle,
    Guid ReporterId,
    int Reason,
    int Status,
    DateTimeOffset CreatedAtUtc);

public sealed record ContentReportDetailsResponse(
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

public sealed record ReviewContentReportRequest(
    int Status,
    string? ResolutionNote);
