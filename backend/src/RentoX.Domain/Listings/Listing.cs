using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings.Enums;

namespace RentoX.Domain.Listings;

public sealed class Listing : AuditableEntity
{
    public const int MaximumImageCount = 30;

    private readonly List<ListingImage> _images = [];

    private readonly List<ListingFieldValue>
    _fieldValues = [];

    private Listing()
    {
    }

    private Listing(
        Guid id,
        Guid ownerId,
        Guid categoryId,
        string title,
        string description,
        decimal price,
        string currency,
        RentalPeriodUnit rentalPeriodUnit)
        : base(id)
    {
        OwnerId = ValidateId(ownerId, "Owner");
        CategoryId = ValidateId(categoryId, "Category");
        Title = ValidateTitle(title);
        Description = ValidateDescription(description);
        Price = ValidatePrice(price);
        Currency = ValidateCurrency(currency);
        RentalPeriodUnit = rentalPeriodUnit;
        Status = ListingStatus.Draft;
    }

    public Guid OwnerId { get; private set; }

    public Guid CategoryId { get; private set; }

    public string Title { get; private set; } =
        string.Empty;

    public string Description { get; private set; } =
        string.Empty;

    public decimal Price { get; private set; }

    public string Currency { get; private set; } = "AZN";

    public RentalPeriodUnit RentalPeriodUnit
    {
        get;
        private set;
    }

    public ListingStatus Status { get; private set; }

    public DateTimeOffset? PublishedAtUtc
    {
        get;
        private set;
    }

    public DateTimeOffset? ExpiresAtUtc
    {
        get;
        private set;
    }

    public string? RejectionReason { get; private set; }

    public IReadOnlyCollection<ListingImage> Images =>
        _images.AsReadOnly();

    public IReadOnlyCollection<ListingFieldValue>
    FieldValues => _fieldValues.AsReadOnly();

    public void AddFieldValue(
    ListingFieldValue fieldValue)
    {
        ArgumentNullException.ThrowIfNull(fieldValue);

        if (fieldValue.ListingId != Id)
        {
            throw new DomainException(
                "Field value does not belong to this listing.");
        }

        if (_fieldValues.Any(value =>
                value.CategoryFieldId ==
                fieldValue.CategoryFieldId))
        {
            throw new DomainException(
                "A value for this field already exists.");
        }

        _fieldValues.Add(fieldValue);
    }

    public static Listing Create(
        Guid ownerId,
        Guid categoryId,
        string title,
        string description,
        decimal price,
        string currency,
        RentalPeriodUnit rentalPeriodUnit)
    {
        if (!Enum.IsDefined(rentalPeriodUnit))
        {
            throw new DomainException(
                "Rental period unit is invalid.");
        }

        return new Listing(
            Guid.NewGuid(),
            ownerId,
            categoryId,
            title,
            description,
            price,
            currency,
            rentalPeriodUnit);
    }

    public ListingImage AddImage(
        string storageKey,
        string contentType,
        long sizeBytes)
    {
        if (_images.Count >= MaximumImageCount)
        {
            throw new DomainException(
                "A listing can contain a maximum of 30 images.");
        }

        int displayOrder = _images.Count;
        bool isCover = _images.Count == 0;

        ListingImage image = ListingImage.Create(
            Id,
            storageKey,
            contentType,
            sizeBytes,
            displayOrder,
            isCover);

        _images.Add(image);

        return image;
    }

    public void SubmitForReview()
    {
        if (_images.Count == 0)
        {
            throw new DomainException(
                "At least one listing image is required.");
        }

        Status = ListingStatus.PendingReview;
        RejectionReason = null;
    }

    public void Publish(
        DateTimeOffset publishedAtUtc,
        TimeSpan lifetime)
    {
        if (lifetime <= TimeSpan.Zero)
        {
            throw new DomainException(
                "Listing lifetime must be positive.");
        }

        Status = ListingStatus.Active;
        PublishedAtUtc = publishedAtUtc;
        ExpiresAtUtc = publishedAtUtc.Add(lifetime);
        RejectionReason = null;
    }

    private static Guid ValidateId(
        Guid value,
        string name)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                $"{name} id is required.");
        }

        return value;
    }

    private static string ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException(
                "Listing title is required.");
        }

        string normalized = title.Trim();

        if (normalized.Length > 150)
        {
            throw new DomainException(
                "Listing title cannot exceed 150 characters.");
        }

        return normalized;
    }

    private static string ValidateDescription(
        string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException(
                "Listing description is required.");
        }

        string normalized = description.Trim();

        if (normalized.Length > 10_000)
        {
            throw new DomainException(
                "Listing description is too long.");
        }

        return normalized;
    }

    private static decimal ValidatePrice(decimal price)
    {
        if (price < 0)
        {
            throw new DomainException(
                "Listing price cannot be negative.");
        }

        return price;
    }

    private static string ValidateCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainException(
                "Currency is required.");
        }

        string normalized =
            currency.Trim().ToUpperInvariant();

        if (normalized.Length != 3)
        {
            throw new DomainException(
                "Currency must contain 3 characters.");
        }

        return normalized;
    }

    public ListingImage RemoveImage(Guid imageId)
    {
        ListingImage image =
            _images.SingleOrDefault(item =>
                item.Id == imageId)
            ?? throw new DomainException(
                "Listing image was not found.");

        bool wasCover = image.IsCover;

        _images.Remove(image);

        List<ListingImage> orderedImages =
            _images
                .OrderBy(item => item.DisplayOrder)
                .ToList();

        for (int index = 0;
             index < orderedImages.Count;
             index++)
        {
            orderedImages[index]
                .ChangeDisplayOrder(index);
        }

        if (wasCover && orderedImages.Count > 0)
        {
            orderedImages[0].SetAsCover();
        }

        return image;
    }

    public void SetCoverImage(Guid imageId)
    {
        ListingImage selectedImage =
            _images.SingleOrDefault(item =>
                item.Id == imageId)
            ?? throw new DomainException(
                "Listing image was not found.");

        foreach (ListingImage image in _images)
        {
            if (image.Id == selectedImage.Id)
            {
                image.SetAsCover();
            }
            else
            {
                image.RemoveCover();
            }
        }
    }

    public void ReorderImages(
        IReadOnlyList<Guid> orderedImageIds)
    {
        ArgumentNullException.ThrowIfNull(
            orderedImageIds);

        if (orderedImageIds.Count != _images.Count)
        {
            throw new DomainException(
                "All listing images must be included.");
        }

        if (orderedImageIds.Distinct().Count() !=
            orderedImageIds.Count)
        {
            throw new DomainException(
                "Image ids must be unique.");
        }

        Dictionary<Guid, ListingImage> imageById =
            _images.ToDictionary(image => image.Id);

        for (int index = 0;
             index < orderedImageIds.Count;
             index++)
        {
            Guid imageId = orderedImageIds[index];

            if (!imageById.TryGetValue(
                    imageId,
                    out ListingImage? image))
            {
                throw new DomainException(
                    "An image does not belong to this listing.");
            }

            image.ChangeDisplayOrder(index);
        }
    }

    public void UpdateDetails(
    string title,
    string description,
    decimal price,
    string currency,
    RentalPeriodUnit rentalPeriodUnit)
    {
        if (Status is not
            (ListingStatus.Draft or
             ListingStatus.Rejected))
        {
            throw new DomainException(
                "Only draft or rejected listings can be edited.");
        }

        if (!Enum.IsDefined(rentalPeriodUnit))
        {
            throw new DomainException(
                "Rental period unit is invalid.");
        }

        Title = ValidateTitle(title);
        Description = ValidateDescription(description);
        Price = ValidatePrice(price);
        Currency = ValidateCurrency(currency);
        RentalPeriodUnit = rentalPeriodUnit;

        Status = ListingStatus.Draft;
        RejectionReason = null;
    }

    public void ReplaceFieldValues(
    IReadOnlyCollection<ListingFieldValue> fieldValues)
    {
        ArgumentNullException.ThrowIfNull(fieldValues);

        if (Status is not
            (ListingStatus.Draft or
             ListingStatus.Rejected))
        {
            throw new DomainException(
                "Only draft or rejected listing fields can be edited.");
        }

        bool duplicateFieldExists =
            fieldValues
                .GroupBy(value =>
                    value.CategoryFieldId)
                .Any(group => group.Count() > 1);

        if (duplicateFieldExists)
        {
            throw new DomainException(
                "A field cannot contain multiple values.");
        }

        if (fieldValues.Any(value =>
                value.ListingId != Id))
        {
            throw new DomainException(
                "A field value does not belong to this listing.");
        }

        _fieldValues.Clear();
        _fieldValues.AddRange(fieldValues);

        Status = ListingStatus.Draft;
        RejectionReason = null;
    }
}