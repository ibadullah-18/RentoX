using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Users.Enums;

namespace RentoX.Domain.Content;

/// <summary>
/// A banner at the top of the home page (site and app): a picture, a short text in three languages
/// and an optional link. It is shown while it is on, has a picture, and today is inside its dates.
/// </summary>
public sealed class HomeBanner : Entity
{
    public const int MaximumTitleLength = 100;

    public const int MaximumSubtitleLength = 200;

    public const int MaximumLinkLength = 500;

    public const int MaximumSlugLength = 60;

    public const int MaximumPageBodyLength = 6000;

    public const int MaximumButtonLabelLength = 40;

    /// <summary>Names the campaign pages cannot take: they are places of the site itself.</summary>
    public static readonly IReadOnlySet<string> ReservedSlugs = new HashSet<string>(StringComparer.Ordinal)
    {
        "admin", "api", "bff", "hubs", "giris", "kabinet", "destek", "elanlarim", "magaza", "magazalar", "mesajlar",
        "pul-kisesi", "bildirisler", "secilmisler", "search", "yeni-elan", "kateqoriyalar", "sikayet", "suallar",
        "haqqimizda", "qaydalar", "sertler", "mexfilik", "emekdasliq", "listing", "listings", "store", "stores",
        "robots", "sitemap", "favicon", "health", "ready", "static", "uploads", "assets", "login", "register",
        "rentoxadminpanel", "campaign", "kampaniya"
    };

    private HomeBanner()
    {
    }

    private HomeBanner(Guid id, Guid createdByUserId, DateTimeOffset createdAtUtc)
        : base(id)
    {
        if (createdByUserId == Guid.Empty)
        {
            throw new DomainException("The person who added the banner is required.");
        }

        if (createdAtUtc == default || createdAtUtc.Offset != TimeSpan.Zero)
        {
            throw new DomainException("The time must be a UTC time.");
        }

        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
    }

    public string TitleAz { get; private set; } = string.Empty;

    public string TitleRu { get; private set; } = string.Empty;

    public string TitleEn { get; private set; } = string.Empty;

    public string? SubtitleAz { get; private set; }

    public string? SubtitleRu { get; private set; }

    public string? SubtitleEn { get; private set; }

    /// <summary>A place on the site ("/destek") or a full address (https://…); empty: the banner is not a link.</summary>
    public string? LinkUrl { get; private set; }

    public string? ImageKey { get; private set; }

    /// <summary>The name of the campaign page after the slash (rentox.az/birbank); null: the campaign has no page.</summary>
    public string? PageSlug { get; private set; }

    public string? PageBodyAz { get; private set; }

    public string? PageBodyRu { get; private set; }

    public string? PageBodyEn { get; private set; }

    public string? PageButtonLabelAz { get; private set; }

    public string? PageButtonLabelRu { get; private set; }

    public string? PageButtonLabelEn { get; private set; }

    public string? PageButtonUrl { get; private set; }

    public bool OpenInNewTab { get; private set; }

    /// <summary>1 website, 2 apps, 3 both.</summary>
    public int Platform { get; private set; } = 3;

    public bool ShowText { get; private set; } = true;

    /// <summary>0 left, 1 centre, 2 right.</summary>
    public int TextAlign { get; private set; }

    public bool DarkOverlay { get; private set; } = true;

    /// <summary>0 light text, 1 dark text.</summary>
    public int TextTone { get; private set; }

    public int DisplayOrder { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset? StartsAtUtc { get; private set; }

    public DateTimeOffset? EndsAtUtc { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static HomeBanner Create(
        string titleAz, string titleRu, string titleEn,
        string? subtitleAz, string? subtitleRu, string? subtitleEn,
        string? linkUrl, int displayOrder, bool isActive,
        DateTimeOffset? startsAtUtc, DateTimeOffset? endsAtUtc,
        Guid createdByUserId, DateTimeOffset createdAtUtc)
    {
        HomeBanner banner = new(Guid.NewGuid(), createdByUserId, createdAtUtc);
        banner.Update(
            titleAz, titleRu, titleEn, subtitleAz, subtitleRu, subtitleEn,
            linkUrl, displayOrder, isActive, startsAtUtc, endsAtUtc);
        return banner;
    }

    public void Update(
        string titleAz, string titleRu, string titleEn,
        string? subtitleAz, string? subtitleRu, string? subtitleEn,
        string? linkUrl, int displayOrder, bool isActive,
        DateTimeOffset? startsAtUtc, DateTimeOffset? endsAtUtc)
    {
        if (displayOrder is < 0 or > 10000)
        {
            throw new DomainException("Display order must be between 0 and 10000.");
        }

        if (startsAtUtc.HasValue && endsAtUtc.HasValue && endsAtUtc.Value <= startsAtUtc.Value)
        {
            throw new DomainException("The end of the banner must be after its start.");
        }

        if ((startsAtUtc.HasValue && startsAtUtc.Value.Offset != TimeSpan.Zero) ||
            (endsAtUtc.HasValue && endsAtUtc.Value.Offset != TimeSpan.Zero))
        {
            throw new DomainException("Banner dates must be UTC times.");
        }

        string newTitleAz = RequireText(titleAz, MaximumTitleLength, "Banner title");
        string newTitleRu = RequireText(titleRu, MaximumTitleLength, "Banner title");
        string newTitleEn = RequireText(titleEn, MaximumTitleLength, "Banner title");
        string? newSubtitleAz = OptionalText(subtitleAz, MaximumSubtitleLength, "Banner subtitle");
        string? newSubtitleRu = OptionalText(subtitleRu, MaximumSubtitleLength, "Banner subtitle");
        string? newSubtitleEn = OptionalText(subtitleEn, MaximumSubtitleLength, "Banner subtitle");
        string? newLink = NormalizeLink(linkUrl);

        TitleAz = newTitleAz;
        TitleRu = newTitleRu;
        TitleEn = newTitleEn;
        SubtitleAz = newSubtitleAz;
        SubtitleRu = newSubtitleRu;
        SubtitleEn = newSubtitleEn;
        LinkUrl = newLink;
        DisplayOrder = displayOrder;
        IsActive = isActive;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
    }

    /// <summary>
    /// Sets the page of the campaign and how it is shown. With a page, the banner leads to it (the link is the page).
    /// </summary>
    public void SetCampaign(BannerCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);

        if (campaign.Platform is < 1 or > 3)
        {
            throw new DomainException("The place where the banner is shown is not valid.");
        }

        if (campaign.TextAlign is < 0 or > 2 || campaign.TextTone is < 0 or > 1)
        {
            throw new DomainException("The look of the banner text is not valid.");
        }

        string? slug = NormalizeSlug(campaign.PageSlug);
        string? bodyAz = OptionalText(campaign.PageBodyAz, MaximumPageBodyLength, "Campaign page text");
        string? bodyRu = OptionalText(campaign.PageBodyRu, MaximumPageBodyLength, "Campaign page text");
        string? bodyEn = OptionalText(campaign.PageBodyEn, MaximumPageBodyLength, "Campaign page text");
        string? labelAz = OptionalText(campaign.PageButtonLabelAz, MaximumButtonLabelLength, "Campaign button label");
        string? labelRu = OptionalText(campaign.PageButtonLabelRu, MaximumButtonLabelLength, "Campaign button label");
        string? labelEn = OptionalText(campaign.PageButtonLabelEn, MaximumButtonLabelLength, "Campaign button label");
        string? buttonUrl = NormalizeLink(campaign.PageButtonUrl);

        if (buttonUrl is not null && (labelAz is null || labelRu is null || labelEn is null))
        {
            throw new DomainException("The button of the campaign page needs its label in all three languages.");
        }

        PageSlug = slug;
        PageBodyAz = bodyAz;
        PageBodyRu = bodyRu;
        PageBodyEn = bodyEn;
        PageButtonLabelAz = labelAz;
        PageButtonLabelRu = labelRu;
        PageButtonLabelEn = labelEn;
        PageButtonUrl = buttonUrl;
        OpenInNewTab = campaign.OpenInNewTab;
        Platform = campaign.Platform;
        ShowText = campaign.ShowText;
        TextAlign = campaign.TextAlign;
        DarkOverlay = campaign.DarkOverlay;
        TextTone = campaign.TextTone;

        // A campaign with its own page leads to that page.
        if (slug is not null)
        {
            LinkUrl = "/" + slug;
        }
    }

    public string? PageBodyFor(PreferredLanguage language) =>
        language switch
        {
            PreferredLanguage.Russian => PageBodyRu,
            PreferredLanguage.English => PageBodyEn,
            _ => PageBodyAz
        };

    public string? PageButtonLabelFor(PreferredLanguage language) =>
        language switch
        {
            PreferredLanguage.Russian => PageButtonLabelRu,
            PreferredLanguage.English => PageButtonLabelEn,
            _ => PageButtonLabelAz
        };

    private static string? NormalizeSlug(string? value)
    {
        string text = (value ?? string.Empty).Trim().Trim('/').ToLowerInvariant();
        if (text.Length == 0)
        {
            return null;
        }

        if (text.Length is < 2 or > MaximumSlugLength)
        {
            throw new DomainException($"The name of the page must have 2 to {MaximumSlugLength} characters.");
        }

        bool valid = text.All(c => c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-') &&
            !text.StartsWith('-') && !text.EndsWith('-') && !text.Contains("--", StringComparison.Ordinal);
        if (!valid)
        {
            throw new DomainException("The name of the page can have only letters a-z, digits and single dashes.");
        }

        if (ReservedSlugs.Contains(text))
        {
            throw new DomainException("This name of the page is reserved by the site.");
        }

        return text;
    }

    public void SetImage(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || storageKey.Length > 300)
        {
            throw new DomainException("Banner image key is invalid.");
        }

        ImageKey = storageKey;
    }

    /// <summary>On show right now: switched on, with a picture, inside its dates.</summary>
    public bool IsLive(DateTimeOffset now) =>
        IsActive &&
        ImageKey is not null &&
        (!StartsAtUtc.HasValue || StartsAtUtc.Value <= now) &&
        (!EndsAtUtc.HasValue || EndsAtUtc.Value > now);

    public string TitleFor(PreferredLanguage language) =>
        language switch
        {
            PreferredLanguage.Russian => TitleRu,
            PreferredLanguage.English => TitleEn,
            _ => TitleAz
        };

    public string? SubtitleFor(PreferredLanguage language) =>
        language switch
        {
            PreferredLanguage.Russian => SubtitleRu,
            PreferredLanguage.English => SubtitleEn,
            _ => SubtitleAz
        };

    private static string RequireText(string? value, int maximum, string field)
    {
        string text = (value ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            throw new DomainException($"{field} is required in all three languages.");
        }

        if (text.Length > maximum)
        {
            throw new DomainException($"{field} cannot exceed {maximum} characters.");
        }

        return text;
    }

    private static string? OptionalText(string? value, int maximum, string field)
    {
        string text = (value ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            return null;
        }

        if (text.Length > maximum)
        {
            throw new DomainException($"{field} cannot exceed {maximum} characters.");
        }

        return text;
    }

    private static string? NormalizeLink(string? value)
    {
        string text = (value ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            return null;
        }

        if (text.Length > MaximumLinkLength)
        {
            throw new DomainException($"Banner link cannot exceed {MaximumLinkLength} characters.");
        }

        bool inSite = text.StartsWith('/') && !text.StartsWith("//", StringComparison.Ordinal);
        bool outside = Uri.TryCreate(text, UriKind.Absolute, out Uri? uri) &&
            (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
        if (!inSite && !outside)
        {
            throw new DomainException("Banner link must be a place on the site (/…) or an http(s) address.");
        }

        return text;
    }
}
