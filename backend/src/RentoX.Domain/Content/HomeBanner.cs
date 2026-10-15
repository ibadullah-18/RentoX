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
