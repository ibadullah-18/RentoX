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