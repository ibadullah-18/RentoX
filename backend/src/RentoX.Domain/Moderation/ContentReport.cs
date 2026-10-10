using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Moderation;

/// <summary>What a report is about.</summary>
public enum ContentReportTarget
{
    Listing = 1,
    Store = 2
}

public enum ContentReportReason
{
    Spam = 1,
    Fraud = 2,
    ProhibitedContent = 3,
    Misleading = 4,
    WrongCategory = 5,
    Other = 6
}

public enum ContentReportStatus
{
    Pending = 1,
    Reviewing = 2,
    Resolved = 3,
    Rejected = 4
}

/// <summary>
/// A user's complaint about a listing or a store, reviewed by a moderator.
/// </summary>
public sealed class ContentReport : Entity
{
    public const int MaximumDetailsLength = 1000;

    private ContentReport()
    {
    }

    private ContentReport(
        Guid id,
        ContentReportTarget targetType,
        Guid targetId,
        Guid reporterId,
        ContentReportReason reason,
        string? details,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        TargetType = targetType;
        TargetId = targetId;
        ReporterId = reporterId;
        Reason = reason;
        Details = details;
        Status = ContentReportStatus.Pending;
        CreatedAtUtc = createdAtUtc;
    }

    public ContentReportTarget TargetType { get; private set; }

    public Guid TargetId { get; private set; }

    public Guid ReporterId { get; private set; }

    public ContentReportReason Reason { get; private set; }

    public string? Details { get; private set; }

    public ContentReportStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? ReviewedByUserId { get; private set; }

    public DateTimeOffset? ReviewedAtUtc { get; private set; }

    public string? ResolutionNote { get; private set; }

    public static ContentReport Create(
        ContentReportTarget targetType,
        Guid targetId,
        Guid reporterId,
        ContentReportReason reason,
        string? details,
        DateTimeOffset createdAtUtc)
    {
        if (!Enum.IsDefined(targetType))
        {
            throw new DomainException("Report target is invalid.");
        }

        if (targetId == Guid.Empty || reporterId == Guid.Empty)
        {
            throw new DomainException("Target and reporter ids are required.");
        }

        if (!Enum.IsDefined(reason))
        {
            throw new DomainException("Report reason is invalid.");
        }

        string? normalized = Normalize(details, "Report details");

        if (reason == ContentReportReason.Other && normalized is null)
        {
            throw new DomainException(
                "Details are required for the Other reason.");
        }

        RequireUtc(createdAtUtc, "Report time");

        return new ContentReport(
            Guid.NewGuid(),
            targetType,
            targetId,
            reporterId,
            reason,
            normalized,
            createdAtUtc);
    }

    public void Review(
        ContentReportStatus status,
        Guid reviewedByUserId,
        string? resolutionNote,
        DateTimeOffset reviewedAtUtc)
    {
        if (Status is ContentReportStatus.Resolved
            or ContentReportStatus.Rejected)
        {
            throw new DomainException(
                "A completed report cannot be changed.");
        }

        if (status is not (ContentReportStatus.Reviewing
            or ContentReportStatus.Resolved
            or ContentReportStatus.Rejected))
        {
            throw new DomainException(
                "Report status transition is invalid.");
        }

        if (reviewedByUserId == Guid.Empty)
        {
            throw new DomainException("Reviewer id is required.");
        }

        RequireUtc(reviewedAtUtc, "Review time");

        string? note = Normalize(resolutionNote, "Resolution note");

        if (status is ContentReportStatus.Resolved
            or ContentReportStatus.Rejected && note is null)
        {
            throw new DomainException(
                "A resolution note is required to complete a report.");
        }

        Status = status;
        ReviewedByUserId = reviewedByUserId;
        ReviewedAtUtc = reviewedAtUtc;
        ResolutionNote = note;
    }

    private static string? Normalize(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalized = value.Trim();

        if (normalized.Length > MaximumDetailsLength)
        {
            throw new DomainException(
                $"{field} cannot exceed {MaximumDetailsLength} characters.");
        }

        return normalized;
    }

    private static void RequireUtc(DateTimeOffset value, string field)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException($"{field} must be UTC.");
        }
    }
}
