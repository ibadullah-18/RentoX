using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Messaging;

public sealed record MessageImageFile(
    string StorageKey, string ContentType, long SizeBytes);

public sealed class MessageImage : Entity
{
    public const long MaximumSizeBytes = 10 * 1024 * 1024;

    private MessageImage() { }

    private MessageImage(Guid id, Guid messageId, MessageImageFile file, int displayOrder)
        : base(id)
    {
        MessageId = messageId;
        StorageKey = file.StorageKey;
        ContentType = file.ContentType;
        SizeBytes = file.SizeBytes;
        DisplayOrder = displayOrder;
    }

    public Guid MessageId { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public int DisplayOrder { get; private set; }

    internal static MessageImage Create(Guid messageId, MessageImageFile file, int displayOrder)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (messageId == Guid.Empty || string.IsNullOrWhiteSpace(file.StorageKey) ||
            file.StorageKey.Length > 500)
        {
            throw new DomainException("Message image metadata is invalid.");
        }
        if (file.SizeBytes is <= 0 or > MaximumSizeBytes)
        {
            throw new DomainException("Each image must be between 1 byte and 10 MB.");
        }
        if (file.ContentType is not ("image/jpeg" or "image/png" or "image/webp"))
        {
            throw new DomainException("Only JPEG, PNG and WebP images are supported.");
        }
        if (displayOrder is < 0 or >= Message.MaximumImageCount)
        {
            throw new DomainException("Message image order is invalid.");
        }
        return new MessageImage(Guid.NewGuid(), messageId, file, displayOrder);
    }
}
