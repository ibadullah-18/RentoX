using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Support;

public sealed class SupportTicketMessage : Entity
{
    private const int MaximumBodyLength = 4000;

    private SupportTicketMessage()
    {
    }

    private SupportTicketMessage(
        Guid id,
        Guid supportTicketId,
        Guid senderId,
        bool isAdmin,
        string body,
        DateTimeOffset sentAtUtc)
        : base(id)
    {
        if (supportTicketId == Guid.Empty)
        {
            throw new DomainException(
                "Support ticket id is required.");
        }

        if (senderId == Guid.Empty)
        {
            throw new DomainException(
                "Support message sender id is required.");
        }

        RequireUtc(sentAtUtc);

        SupportTicketId = supportTicketId;
        SenderId = senderId;
        IsAdmin = isAdmin;
        Body = NormalizeBody(body);
        SentAtUtc = sentAtUtc;
    }

    public Guid SupportTicketId { get; private set; }

    public Guid SenderId { get; private set; }

    public bool IsAdmin { get; private set; }

    public string Body { get; private set; } =
        string.Empty;

    public DateTimeOffset SentAtUtc { get; private set; }

    public static SupportTicketMessage Create(
        Guid supportTicketId,
        Guid senderId,
        bool isAdmin,
        string body,
        DateTimeOffset sentAtUtc)
    {
        return new SupportTicketMessage(
            Guid.NewGuid(),
            supportTicketId,
            senderId,
            isAdmin,
            body,
            sentAtUtc);
    }

    private static string NormalizeBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new DomainException(
                "Support message body is required.");
        }

        string normalized = body.Trim();

        if (normalized.Length > MaximumBodyLength)
        {
            throw new DomainException(
                $"Support message body cannot exceed {MaximumBodyLength} characters.");
        }

        return normalized;
    }

    private static void RequireUtc(
        DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(
                "Support message sent time must be UTC.");
        }
    }
}