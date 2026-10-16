using System.Net.Mail;
using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Stores.Enums;

namespace RentoX.Domain.Stores;

public sealed class StoreProfile : AuditableEntity
{
    private StoreProfile()
    {
    }

    private StoreProfile(
        Guid id,
        Guid ownerId,
        string name,
        string slug,
        string description,
        string phoneNumber,
        string? email,
        string? address,
        string? instagramUrl,
        string? tiktokUrl,
        string? facebookUrl,
        string? websiteUrl)
        : base(id)
    {
        if (ownerId == Guid.Empty)
        {
            throw new DomainException(
                "Store owner id is required.");
        }

        OwnerId = ownerId;
        Name = ValidateRequiredText(
            name,
            "Store name",
            2,
            100);

        Slug = ValidateSlug(slug);

        Description = ValidateRequiredText(
            description,
            "Store description",
            10,
            2000);

        PhoneNumber = ValidateRequiredText(
            phoneNumber,
            "Store phone number",
            7,
            30);

        Email = ValidateEmail(email);
        Address = NormalizeOptionalText(address, 500);
        InstagramUrl = ValidateUrl(instagramUrl);
        TiktokUrl = ValidateUrl(tiktokUrl);
        FacebookUrl = ValidateUrl(facebookUrl);
        WebsiteUrl = ValidateUrl(websiteUrl);
        Status = StoreStatus.Draft;
    }

    public Guid OwnerId { get; private set; }

    public string Name { get; private set; } =
        string.Empty;

    public string Slug { get; private set; } =
        string.Empty;

    public string Description { get; private set; } =
        string.Empty;

    public string PhoneNumber { get; private set; } =
        string.Empty;

    public string? Email { get; private set; }

    public string? Address { get; private set; }

    public string? LogoImageKey { get; private set; }

    public string? CoverImageKey { get; private set; }

    public string? InstagramUrl { get; private set; }

    public string? TiktokUrl { get; private set; }

    public string? FacebookUrl { get; private set; }

    public string? WebsiteUrl { get; private set; }

    public string? YoutubeUrl { get; private set; }

    /// <summary>The WhatsApp number in international form, digits only (for example 994501234567).</summary>
    public string? WhatsAppNumber { get; private set; }

    /// <summary>The first message a visitor's WhatsApp opens with; the owner decides what it says.</summary>
    public string? WhatsAppMessage { get; private set; }

    /// <summary>The open days and hours as kept text; read it with <see cref="StoreWorkingHours.Read"/>.</summary>
    public string? WorkingHours { get; private set; }

    public StoreStatus Status { get; private set; }

    public string? RejectionReason { get; private set; }

    public static StoreProfile Create(
        Guid ownerId,
        string name,
        string slug,
        string description,
        string phoneNumber,
        string? email,
        string? address,
        string? instagramUrl,
        string? tiktokUrl,
        string? facebookUrl,
        string? websiteUrl)
    {
        return new StoreProfile(
            Guid.NewGuid(),
            ownerId,
            name,
            slug,
            description,
            phoneNumber,
            email,
            address,
            instagramUrl,
            tiktokUrl,
            facebookUrl,
            websiteUrl);
    }

    public void Update(
        string name,
        string description,
        string phoneNumber,
        string? email,
        string? address,
        string? instagramUrl,
        string? tiktokUrl,
        string? facebookUrl,
        string? websiteUrl)
    {
        if (Status is not
            (StoreStatus.Draft or
             StoreStatus.Rejected))
        {
            throw new DomainException(
                "Only draft or rejected stores can be edited.");
        }

        Name = ValidateRequiredText(
            name,
            "Store name",
            2,
            100);

        Description = ValidateRequiredText(
            description,
            "Store description",
            10,
            2000);

        PhoneNumber = ValidateRequiredText(
            phoneNumber,
            "Store phone number",
            7,
            30);

        Email = ValidateEmail(email);
        Address = NormalizeOptionalText(address, 500);
        InstagramUrl = ValidateUrl(instagramUrl);
        TiktokUrl = ValidateUrl(tiktokUrl);
        FacebookUrl = ValidateUrl(facebookUrl);
        WebsiteUrl = ValidateUrl(websiteUrl);
        Status = StoreStatus.Draft;
        RejectionReason = null;
    }

    /// <summary>
    /// The name and the description of a LIVE store. The owner may rewrite them at any time (the store stays live and
    /// the address does not change); a moderator can still suspend the store if the new text is not acceptable.
    /// </summary>
    public void UpdateInfo(string name, string description)
    {
        if (Status != StoreStatus.Active)
        {
            throw new DomainException(
                "Only a live store can be edited this way.");
        }

        string newName = ValidateRequiredText(name, "Store name", 2, 100);
        string newDescription = ValidateRequiredText(description, "Store description", 10, 2000);
        Name = newName;
        Description = newDescription;
    }

    /// <summary>
    /// The contact side of the store: phone, e-mail, address, social media, WhatsApp and the hours. The owner can
    /// change it at any time (a live store stays live): it is not what the moderators approved. Everything is checked
    /// first, then set.
    /// </summary>
    public void UpdateContact(
        string phoneNumber,
        string? email,
        string? address,
        string? instagramUrl,
        string? tiktokUrl,
        string? facebookUrl,
        string? youtubeUrl,
        string? websiteUrl,
        string? whatsAppNumber,
        string? whatsAppMessage,
        IReadOnlyCollection<StoreOpenDay>? workingHours)
    {
        if (Status is StoreStatus.Deleted or StoreStatus.Suspended)
        {
            throw new DomainException(
                "This store cannot be changed now.");
        }

        string phone = ValidateRequiredText(phoneNumber, "Store phone number", 7, 30);
        string? mail = ValidateEmail(email);
        string? place = NormalizeOptionalText(address, 500);
        string? instagram = ValidateUrl(instagramUrl);
        string? tiktok = ValidateUrl(tiktokUrl);
        string? facebook = ValidateUrl(facebookUrl);
        string? youtube = ValidateUrl(youtubeUrl);
        string? website = ValidateUrl(websiteUrl);
        string? whatsApp = NormalizeWhatsApp(whatsAppNumber);
        string? greeting = NormalizeOptionalText(whatsAppMessage, 300);
        string? hours = StoreWorkingHours.Serialize(workingHours);

        PhoneNumber = phone;
        Email = mail;
        Address = place;
        InstagramUrl = instagram;
        TiktokUrl = tiktok;
        FacebookUrl = facebook;
        YoutubeUrl = youtube;
        WebsiteUrl = website;
        WhatsAppNumber = whatsApp;
        WhatsAppMessage = whatsApp is null ? null : greeting;
        WorkingHours = hours;
    }

    /// <summary>
    /// "+994 50 123 45 67", "050 123 45 67" and "501234567" all become 994501234567 (a local number is an
    /// Azerbaijani one). Anything that is not 9 to 15 digits is refused.
    /// </summary>
    public static string? NormalizeWhatsApp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string digits = new(value.Where(char.IsAsciiDigit).ToArray());

        if (digits.Length == 10 && digits[0] == '0')
        {
            digits = "994" + digits[1..];
        }
        else if (digits.Length == 9)
        {
            digits = "994" + digits;
        }

        if (digits.Length is < 9 or > 15)
        {
            throw new DomainException(
                "WhatsApp number is invalid.");
        }

        return digits;
    }

    public void SetLogo(string? storageKey)
    {
        EnsureEditable();

        LogoImageKey =
            NormalizeOptionalText(storageKey, 500);
    }

    public void SetCover(string? storageKey)
    {
        EnsureEditable();

        CoverImageKey =
            NormalizeOptionalText(storageKey, 500);
    }

    public void SubmitForReview()
    {
        if (Status is not
            (StoreStatus.Draft or
             StoreStatus.Rejected))
        {
            throw new DomainException(
                "Only draft or rejected stores can be submitted.");
        }

        if (string.IsNullOrWhiteSpace(LogoImageKey))
        {
            throw new DomainException(
                "Store logo is required.");
        }

        Status = StoreStatus.PendingReview;
        RejectionReason = null;
    }

    public void Approve()
    {
        if (Status != StoreStatus.PendingReview)
        {
            throw new DomainException(
                "Only stores pending review can be approved.");
        }

        Status = StoreStatus.Active;
        RejectionReason = null;
    }

    public void Reject(string reason)
    {
        if (Status != StoreStatus.PendingReview)
        {
            throw new DomainException(
                "Only stores pending review can be rejected.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                "Store rejection reason is required.");
        }

        string normalized = reason.Trim();

        if (normalized.Length > 1000)
        {
            throw new DomainException(
                "Store rejection reason cannot exceed 1000 characters.");
        }

        Status = StoreStatus.Rejected;
        RejectionReason = normalized;
    }

    /// <summary>
    /// An admin suspends a live store (a complaint, a ban). It leaves the
    /// public lists at once; its owner sees the reason and can write to
    /// support. Only an admin can bring it back.
    /// </summary>
    public void Suspend(string reason)
    {
        if (Status != StoreStatus.Active)
        {
            throw new DomainException(
                "Only active stores can be suspended.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                "Store suspension reason is required.");
        }

        string normalized = reason.Trim();

        if (normalized.Length > 1000)
        {
            throw new DomainException(
                "Store suspension reason cannot exceed 1000 characters.");
        }

        Status = StoreStatus.Suspended;
        RejectionReason = normalized;
    }

    /// <summary>Brings a suspended store back to the public lists.</summary>
    public void Reinstate()
    {
        if (Status != StoreStatus.Suspended)
        {
            throw new DomainException(
                "Only suspended stores can be reinstated.");
        }

        Status = StoreStatus.Active;
        RejectionReason = null;
    }

    /// <summary>Takes the store out of view for good (owner left).</summary>
    public void MarkDeleted(DateTimeOffset deletedAtUtc)
    {
        Status = StoreStatus.Deleted;
        MarkAsDeleted(deletedAtUtc);
    }

    private void EnsureEditable()
    {
        if (Status is not
            (StoreStatus.Draft or
             StoreStatus.Rejected))
        {
            throw new DomainException(
                "Only draft or rejected stores can be edited.");
        }
    }
    private static string ValidateRequiredText(
        string value,
        string name,
        int minimumLength,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                $"{name} is required.");
        }

        string normalized = value.Trim();

        if (normalized.Length < minimumLength ||
            normalized.Length > maximumLength)
        {
            throw new DomainException(
                $"{name} must contain between " +
                $"{minimumLength} and " +
                $"{maximumLength} characters.");
        }

        return normalized;
    }

    private static string ValidateSlug(string slug)
    {
        string normalized =
            ValidateRequiredText(
                slug,
                "Store slug",
                2,
                120);

        bool isValid =
            normalized.All(character =>
                character is >= 'a' and <= 'z' ||
                character is >= '0' and <= '9' ||
                character == '-');

        if (!isValid ||
            normalized.StartsWith('-') ||
            normalized.EndsWith('-') ||
            normalized.Contains(
                "--",
                StringComparison.Ordinal))
        {
            throw new DomainException(
                "Store slug is invalid.");
        }

        return normalized;
    }

    private static string? ValidateEmail(string? email)
    {
        string? normalized =
            NormalizeOptionalText(email, 254);

        if (normalized is null)
        {
            return null;
        }

        if (!MailAddress.TryCreate(
                normalized,
                out MailAddress? address))
        {
            throw new DomainException(
                "Store email is invalid.");
        }

        return address.Address;
    }

    private static string? ValidateUrl(string? value)
    {
        string? normalized =
            NormalizeOptionalText(value, 300);

        if (normalized is null)
        {
            return null;
        }

        if (!Uri.TryCreate(
                normalized,
                UriKind.Absolute,
                out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new DomainException(
                "Social media URL is invalid.");
        }

        return uri.AbsoluteUri;
    }

    private static string? NormalizeOptionalText(
        string? value,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalized = value.Trim();

        if (normalized.Length > maximumLength)
        {
            throw new DomainException(
                $"Value cannot exceed " +
                $"{maximumLength} characters.");
        }

        return normalized;
    }
}