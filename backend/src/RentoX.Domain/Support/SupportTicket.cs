using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Support;

public enum SupportTicketCategory
{
    General = 1,
    Account = 2,
    Listing = 3,
    Payment = 4,
    Store = 5,
    Technical = 6,
    Other = 7
}

public enum SupportTicketPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Urgent = 4
}

public enum SupportTicketStatus
{
    Open = 1,
    InProgress = 2,
    Resolved = 3,
    Closed = 4
}

public sealed class SupportTicket : AggregateRoot
{
    private const int MaximumSubjectLength = 160;

    private readonly List<SupportTicketMessage>
        messages = [];

    private SupportTicket()
    {
    }

    private SupportTicket(
        Guid id,
        Guid userId,
        SupportTicketCategory category,
        string subject,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException(
                "Support ticket user id is required.");
        }

        RequireCategory(category);
        RequireUtc(
            createdAtUtc,
            "Support ticket creation time");

        UserId = userId;
        Category = category;
        Subject = NormalizeSubject(subject);
        Priority = SupportTicketPriority.Normal;
        Status = SupportTicketStatus.Open;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid UserId { get; private set; }

    public SupportTicketCategory Category { get; private set; }

    public string Subject { get; private set; } =
        string.Empty;

    public SupportTicketPriority Priority { get; private set; }

    public SupportTicketStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public DateTimeOffset? ResolvedAtUtc { get; private set; }

    public DateTimeOffset? ClosedAtUtc { get; private set; }

    public IReadOnlyCollection<SupportTicketMessage> Messages =>
        messages.AsReadOnly();

    public static SupportTicket Create(
        Guid userId,
        SupportTicketCategory category,
        string subject,
        string initialMessage,
        DateTimeOffset createdAtUtc)
    {
        SupportTicket ticket =
            new(
                Guid.NewGuid(),
                userId,
                category,
                subject,
                createdAtUtc);

        ticket.messages.Add(
            SupportTicketMessage.Create(
                ticket.Id,
                userId,
                false,
                initialMessage,
                createdAtUtc));

        return ticket;
    }

    public void AddUserMessage(
        Guid userId,
        string body,
        DateTimeOffset sentAtUtc)
    {
        EnsureNotClosed();

        if (userId == Guid.Empty ||
            userId != UserId)
        {
            throw new DomainException(
                "Only the ticket owner can add a user message.");
        }

        RequireUtc(
            sentAtUtc,
            "Support message sent time");

        if (Status == SupportTicketStatus.Resolved)
        {
            Status = SupportTicketStatus.Open;
            ResolvedAtUtc = null;
        }

        messages.Add(
            SupportTicketMessage.Create(
                Id,
                userId,
                false,
                body,
                sentAtUtc));

        UpdatedAtUtc = sentAtUtc;
    }

    public void AddAdminMessage(
        Guid adminUserId,
        string body,
        DateTimeOffset sentAtUtc)
    {
        EnsureNotClosed();

        if (adminUserId == Guid.Empty)
        {
            throw new DomainException(
                "Support administrator id is required.");
        }

        RequireUtc(
            sentAtUtc,
            "Support message sent time");

        if (Status is
            SupportTicketStatus.Open or
            SupportTicketStatus.Resolved)
        {
            Status = SupportTicketStatus.InProgress;
            ResolvedAtUtc = null;
        }

        messages.Add(
            SupportTicketMessage.Create(
                Id,
                adminUserId,
                true,
                body,
                sentAtUtc));

        UpdatedAtUtc = sentAtUtc;
    }

    public void ChangeStatus(
        SupportTicketStatus status,
        DateTimeOffset changedAtUtc)
    {
        if (!Enum.IsDefined(status))
        {
            throw new DomainException(
                "Support ticket status is invalid.");
        }

        RequireUtc(
            changedAtUtc,
            "Support status change time");

        Status = status;
        UpdatedAtUtc = changedAtUtc;

        switch (status)
        {
            case SupportTicketStatus.Open:
            case SupportTicketStatus.InProgress:
                ResolvedAtUtc = null;
                ClosedAtUtc = null;
                break;

            case SupportTicketStatus.Resolved:
                ResolvedAtUtc = changedAtUtc;
                ClosedAtUtc = null;
                break;

            case SupportTicketStatus.Closed:
                ClosedAtUtc = changedAtUtc;
                break;

            default:
                throw new DomainException(
                    "Support ticket status is invalid.");
        }
    }

    public void ChangePriority(
        SupportTicketPriority priority,
        DateTimeOffset changedAtUtc)
    {
        if (!Enum.IsDefined(priority))
        {
            throw new DomainException(
                "Support ticket priority is invalid.");
        }

        RequireUtc(
            changedAtUtc,
            "Support priority change time");

        Priority = priority;
        UpdatedAtUtc = changedAtUtc;
    }

    private void EnsureNotClosed()
    {
        if (Status == SupportTicketStatus.Closed)
        {
            throw new DomainException(
                "A closed support ticket cannot receive messages.");
        }
    }

    private static void RequireCategory(
        SupportTicketCategory category)
    {
        if (!Enum.IsDefined(category))
        {
            throw new DomainException(
                "Support ticket category is invalid.");
        }
    }

    private static string NormalizeSubject(
        string subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new DomainException(
                "Support ticket subject is required.");
        }

        string normalized = subject.Trim();

        if (normalized.Length > MaximumSubjectLength)
        {
            throw new DomainException(
                $"Support ticket subject cannot exceed {MaximumSubjectLength} characters.");
        }

        return normalized;
    }

    private static void RequireUtc(
        DateTimeOffset value,
        string field)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(
                $"{field} must be UTC.");
        }
    }
}