using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Support;

public sealed record SupportTicketImageFile(string StorageKey, string ContentType, long SizeBytes);

/// <summary>A photo (a screenshot, a receipt) sent inside a message of a support request, by either side.</summary>
public sealed class SupportTicketImage : Entity
{
    public const long MaximumSizeBytes = 10 * 1024 * 1024;

    public const int MaximumPerMessage = 5;

    private SupportTicketImage()
    {
    }

    private SupportTicketImage(Guid id, Guid messageId, SupportTicketImageFile file, int displayOrder)
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

    internal static SupportTicketImage Create(Guid messageId, SupportTicketImageFile file, int displayOrder)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (messageId == Guid.Empty || string.IsNullOrWhiteSpace(file.StorageKey) || file.StorageKey.Length > 500)
        {
            throw new DomainException("Support image metadata is invalid.");
        }

        if (file.SizeBytes is <= 0 or > MaximumSizeBytes)
        {
            throw new DomainException("Each image must be between 1 byte and 10 MB.");
        }

        if (file.ContentType is not ("image/jpeg" or "image/png" or "image/webp"))
        {
            throw new DomainException("Only JPEG, PNG and WebP images are supported.");
        }

        if (displayOrder is < 0 or >= MaximumPerMessage)
        {
            throw new DomainException("Support image order is invalid.");
        }

        return new SupportTicketImage(Guid.NewGuid(), messageId, file, displayOrder);
    }
}
