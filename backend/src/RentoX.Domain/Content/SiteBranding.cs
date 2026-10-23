using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Content;

/// <summary>The pictures of the brand that the owner can replace in the admin panel.</summary>
public enum BrandingImageSlot
{
    /// <summary>The logo in the header of the site and the apps (on light pages).</summary>
    HeaderLogo = 1,

    /// <summary>The same logo made for dark pages. Empty: the light one is used.</summary>
    HeaderLogoDark = 2,

    /// <summary>The logo in the footer. Empty: the header logo is used.</summary>
    FooterLogo = 3,

    FooterLogoDark = 4,

    /// <summary>The small square icon: the tab icon of the browser and the icon inside the apps.</summary>
    Icon = 5,

    /// <summary>The picture shown when a link to the site is shared (Facebook, WhatsApp, Telegram...). 1200x630 is best.</summary>
    ShareImage = 6
}

/// <summary>
/// How the site looks to those who see it: its name, its logo and icon, the sizes of the logo, the text of the footer.
/// One row. Every picture is optional: while it is empty, the ones that come with the site are shown.
/// </summary>
public sealed class SiteBranding : Entity
{
    public const int MaximumNameLength = 40;

    public const int MaximumFooterTextLength = 200;

    public const int MinimumLogoHeight = 20;

    public const int MaximumLogoHeight = 96;

    public const int DefaultLogoHeight = 40;

    public const int MaximumDescriptionLength = 300;

    public const int MaximumAnnouncementLength = 200;

    public const int MaximumAnnouncementLinkLength = 300;

    private SiteBranding()
    {
    }

    private SiteBranding(Guid id)
        : base(id)
    {
    }

    public string SiteName { get; private set; } = "RentoX";

    public string? FooterText { get; private set; }

    /// <summary>Show the name as plain text instead of a picture (the old look).</summary>
    public bool UseTextLogo { get; private set; }

    public int HeaderLogoHeight { get; private set; } = DefaultLogoHeight;

    public int FooterLogoHeight { get; private set; } = DefaultLogoHeight;

    public string? HeaderLogoKey { get; private set; }

    public string? HeaderLogoDarkKey { get; private set; }

    public string? FooterLogoKey { get; private set; }

    public string? FooterLogoDarkKey { get; private set; }

    public string? IconKey { get; private set; }

    public string? ShareImageKey { get; private set; }

    /// <summary>How much the site moves: 0 full, 1 soft (fades only, no depth), 2 off.</summary>
    public int MotionLevel { get; private set; }

    /// <summary>The look a new visitor gets before they choose: 0 day, 1 night, 2 follow the device.</summary>
    public int DefaultTheme { get; private set; }

    /// <summary>The short description of the site for search engines and shared links. Empty: the standard text.</summary>
    public string? Description { get; private set; }

    public bool AnnouncementEnabled { get; private set; }

    public string? AnnouncementText { get; private set; }

    /// <summary>A place on the site ("/destek") or an http(s) address; empty: the notice is not a link.</summary>
    public string? AnnouncementLink { get; private set; }

    /// <summary>0 information (blue), 1 warning (amber), 2 good news (green).</summary>
    public int AnnouncementTone { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public static SiteBranding CreateDefault() => new(Guid.NewGuid());

    public void Update(
        string siteName, string? footerText, bool useTextLogo, int headerLogoHeight, int footerLogoHeight,
        int motionLevel, int defaultTheme, string? description,
        bool announcementEnabled, string? announcementText, string? announcementLink, int announcementTone,
        DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        RequireUtc(nowUtc);

        if (motionLevel is < 0 or > 2 || defaultTheme is < 0 or > 2 || announcementTone is < 0 or > 2)
        {
            throw new DomainException("The choice is not valid.");
        }

        string? about = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (about is not null && about.Length > MaximumDescriptionLength)
        {
            throw new DomainException($"The description cannot exceed {MaximumDescriptionLength} characters.");
        }

        string? notice = string.IsNullOrWhiteSpace(announcementText) ? null : announcementText.Trim();
        if (notice is not null && notice.Length > MaximumAnnouncementLength)
        {
            throw new DomainException($"The notice cannot exceed {MaximumAnnouncementLength} characters.");
        }

        if (announcementEnabled && notice is null)
        {
            throw new DomainException("The notice needs its text before it is shown.");
        }

        string? link = string.IsNullOrWhiteSpace(announcementLink) ? null : announcementLink.Trim();
        if (link is not null)
        {
            bool inSite = link.StartsWith('/') && !link.StartsWith("//", StringComparison.Ordinal);
            bool outside = Uri.TryCreate(link, UriKind.Absolute, out Uri? uri) &&
                (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
            if ((!inSite && !outside) || link.Length > MaximumAnnouncementLinkLength)
            {
                throw new DomainException("The link of the notice must be a place on the site (/...) or an http(s) address.");
            }
        }

        string name = (siteName ?? string.Empty).Trim();
        if (name.Length == 0 || name.Length > MaximumNameLength)
        {
            throw new DomainException($"The name of the site must have 1 to {MaximumNameLength} characters.");
        }

        string? footer = string.IsNullOrWhiteSpace(footerText) ? null : footerText.Trim();
        if (footer is not null && footer.Length > MaximumFooterTextLength)
        {
            throw new DomainException($"The footer text cannot exceed {MaximumFooterTextLength} characters.");
        }

        if (headerLogoHeight is < MinimumLogoHeight or > MaximumLogoHeight ||
            footerLogoHeight is < MinimumLogoHeight or > MaximumLogoHeight)
        {
            throw new DomainException($"The height of the logo must be between {MinimumLogoHeight} and {MaximumLogoHeight}.");
        }

        MotionLevel = motionLevel;
        DefaultTheme = defaultTheme;
        Description = about;
        AnnouncementEnabled = announcementEnabled;
        AnnouncementText = notice;
        AnnouncementLink = link;
        AnnouncementTone = announcementTone;
        SiteName = name;
        FooterText = footer;
        UseTextLogo = useTextLogo;
        HeaderLogoHeight = headerLogoHeight;
        FooterLogoHeight = footerLogoHeight;
        Touch(nowUtc, updatedByUserId);
    }

    public string? KeyOf(BrandingImageSlot slot) => slot switch
    {
        BrandingImageSlot.HeaderLogo => HeaderLogoKey,
        BrandingImageSlot.HeaderLogoDark => HeaderLogoDarkKey,
        BrandingImageSlot.FooterLogo => FooterLogoKey,
        BrandingImageSlot.FooterLogoDark => FooterLogoDarkKey,
        BrandingImageSlot.Icon => IconKey,
        BrandingImageSlot.ShareImage => ShareImageKey,
        _ => throw new DomainException("The picture is not known.")
    };

    /// <summary>Puts a picture in its place; returns the key of the one it replaced (to delete its file).</summary>
    public string? SetImage(BrandingImageSlot slot, string? storageKey, DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        RequireUtc(nowUtc);
        if (storageKey is not null && (string.IsNullOrWhiteSpace(storageKey) || storageKey.Length > 300))
        {
            throw new DomainException("The picture key is invalid.");
        }

        string? previous = KeyOf(slot);
        switch (slot)
        {
            case BrandingImageSlot.HeaderLogo:
                HeaderLogoKey = storageKey;
                break;
            case BrandingImageSlot.HeaderLogoDark:
                HeaderLogoDarkKey = storageKey;
                break;
            case BrandingImageSlot.FooterLogo:
                FooterLogoKey = storageKey;
                break;
            case BrandingImageSlot.FooterLogoDark:
                FooterLogoDarkKey = storageKey;
                break;
            case BrandingImageSlot.Icon:
                IconKey = storageKey;
                break;
            case BrandingImageSlot.ShareImage:
                ShareImageKey = storageKey;
                break;
            default:
                throw new DomainException("The picture is not known.");
        }

        Touch(nowUtc, updatedByUserId);
        return previous;
    }

    private void Touch(DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        if (updatedByUserId == Guid.Empty)
        {
            throw new DomainException("The person who made the change is invalid.");
        }

        UpdatedAtUtc = nowUtc;
        UpdatedByUserId = updatedByUserId;
    }

    private static void RequireUtc(DateTimeOffset value)
    {
        if (value == default || value.Offset != TimeSpan.Zero)
        {
            throw new DomainException("The time must be a UTC time.");
        }
    }
}
