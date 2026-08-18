using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Listings;

public sealed class ListingImage : Entity
{
    private ListingImage()
    {
    }

    private ListingImage(
        Guid id,
        Guid listingId,
        string storageKey,
        string contentType,
        long sizeBytes,
        int displayOrder,
        bool isCover)
        : base(id)
    {
        ListingId = listingId;
        StorageKey = ValidateRequired(
            storageKey,
            "Image storage key");

        ContentType = ValidateRequired(
            contentType,
            "Image content type");

        SizeBytes = ValidateSize(sizeBytes);
        DisplayOrder = ValidateOrder(displayOrder);
        IsCover = isCover;
    }

    public Guid ListingId { get; private set; }

    public string StorageKey { get; private set; } =
        string.Empty;

    public string ContentType { get; private set; } =
        string.Empty;

    public long SizeBytes { get; private set; }

    public int DisplayOrder { get; private set; }

    public bool IsCover { get; private set; }

    public static ListingImage Create(
        Guid listingId,
        string storageKey,
        string contentType,
        long sizeBytes,
        int displayOrder,
        bool isCover)
    {
        if (listingId == Guid.Empty)
        {
            throw new DomainException(
                "Listing id is required.");
        }

        return new ListingImage(
            Guid.NewGuid(),
            listingId,
            storageKey,
            contentType,
            sizeBytes,
            displayOrder,
            isCover);
    }

    public void SetAsCover()
    {
        IsCover = true;
    }

    public void RemoveCover()
    {
        IsCover = false;
    }

    private static string ValidateRequired(
        string value,
        string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                $"{name} is required.");
        }

        return value.Trim();
    }

    private static long ValidateSize(long sizeBytes)
    {
        if (sizeBytes <= 0)
        {
            throw new DomainException(
                "Image size must be positive.");
        }

        return sizeBytes;
    }

    private static int ValidateOrder(int displayOrder)
    {
        if (displayOrder < 0)
        {
            throw new DomainException(
                "Display order cannot be negative.");
        }

        return displayOrder;
    }

    public void ChangeDisplayOrder(int displayOrder)
    {
        DisplayOrder = ValidateOrder(displayOrder);
    }
}