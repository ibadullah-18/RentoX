using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Messaging;

public sealed class Message : Entity
{
    private Message()
    {
    }

    private Message(
        Guid id,
        Guid conversationId,
        Guid senderId,
        string body,
        DateTimeOffset sentAtUtc)
        : base(id)
    {
        if (conversationId == Guid.Empty ||
            senderId == Guid.Empty)
        {
            throw new DomainException(
                "Message conversation and sender ids are required.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new DomainException(
                "Message text is required.");
        }

        string normalizedBody = body.Trim();

        if (normalizedBody.Length > 2000)
        {
            throw new DomainException(
                "Message cannot exceed 2000 characters.");
        }

        if (sentAtUtc.Offset != TimeSpan.Zero)
        {
            throw new DomainException(
                "Message time must be UTC.");
        }

        ConversationId = conversationId;
        SenderId = senderId;
        Body = normalizedBody;
        SentAtUtc = sentAtUtc;
    }

    public Guid ConversationId { get; private set; }

    public Guid SenderId { get; private set; }

    public string Body { get; private set; } = string.Empty;

    public DateTimeOffset SentAtUtc { get; private set; }

    public DateTimeOffset? ReadAtUtc { get; private set; }

    public static Message Create(
        Guid conversationId,
        Guid senderId,
        string body,
        DateTimeOffset sentAtUtc)
    {
        return new Message(
            Guid.NewGuid(),
            conversationId,
            senderId,
            body,
            sentAtUtc);
    }

    public void MarkRead(DateTimeOffset readAtUtc)
    {
        if (ReadAtUtc.HasValue)
        {
            return;
        }

        if (readAtUtc.Offset != TimeSpan.Zero ||
            readAtUtc < SentAtUtc)
        {
            throw new DomainException(
                "Message read time is invalid.");
        }

        ReadAtUtc = readAtUtc;
    }
}