using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Messaging;

public enum ConversationReportReason
{
    Spam = 1,
    Fraud = 2,
    Harassment = 3,
    ProhibitedContent = 4,
    Other = 5
}

public enum ConversationReportStatus
{
    Pending = 1,
    Reviewing = 2,
    Resolved = 3,
    Rejected = 4
}

public sealed class ConversationReport : Entity
{
    private ConversationReport() { }

    private ConversationReport(
        Guid id,
        Guid conversationId,
        Guid reporterId,
        ConversationReportReason reason,
        string? details,
        Guid? evidenceMessageId,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        ConversationId = conversationId;
        ReporterId = reporterId;
        Reason = reason;
        Details = NormalizeOptional(details, 1000, "Report details");
        EvidenceMessageId = evidenceMessageId;
        Status = ConversationReportStatus.Pending;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid ConversationId { get; private set; }
    public Guid ReporterId { get; private set; }
    public ConversationReportReason Reason { get; private set; }
    public string? Details { get; private set; }
    public Guid? EvidenceMessageId { get; private set; }
    public ConversationReportStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public string? ResolutionNote { get; private set; }

    public static ConversationReport Create(
        Guid conversationId,
        Guid reporterId,
        ConversationReportReason reason,
        string? details,
        Guid? evidenceMessageId,
        DateTimeOffset createdAtUtc)
    {
        if (conversationId == Guid.Empty || reporterId == Guid.Empty)
        {
            throw new DomainException("Conversation and reporter ids are required.");
        }
        if (!Enum.IsDefined(reason))
        {
            throw new DomainException("Report reason is invalid.");
        }
        if (reason == ConversationReportReason.Other && string.IsNullOrWhiteSpace(details))
        {
            throw new DomainException("Details are required for the Other reason.");
        }
        RequireUtc(createdAtUtc, "Report time");
        return new ConversationReport(
            Guid.NewGuid(), conversationId, reporterId, reason,
            details, evidenceMessageId, createdAtUtc);
    }

    public void Review(
        ConversationReportStatus status,
        Guid reviewedByUserId,
        string? resolutionNote,
        DateTimeOffset reviewedAtUtc)
    {
        if (Status is ConversationReportStatus.Resolved or ConversationReportStatus.Rejected)
        {
            throw new DomainException("A completed report cannot be changed.");
        }
        if (status is not (ConversationReportStatus.Reviewing or
            ConversationReportStatus.Resolved or ConversationReportStatus.Rejected))
        {
            throw new DomainException("Report status transition is invalid.");
        }
        if (reviewedByUserId == Guid.Empty)
        {
            throw new DomainException("Reviewer id is required.");
        }
        RequireUtc(reviewedAtUtc, "Review time");

        string? note = NormalizeOptional(resolutionNote, 1000, "Resolution note");
        if (status is ConversationReportStatus.Resolved or ConversationReportStatus.Rejected &&
            note is null)
        {
            throw new DomainException("A resolution note is required to complete a report.");
        }

        Status = status;
        ReviewedByUserId = reviewedByUserId;
        ReviewedAtUtc = reviewedAtUtc;
        ResolutionNote = note;
    }

    private static string? NormalizeOptional(string? value, int maximumLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) { return null; }
        string normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new DomainException($"{field} cannot exceed {maximumLength} characters.");
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
