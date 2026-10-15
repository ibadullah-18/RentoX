using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Users.Enums;

namespace RentoX.Domain.Content;

/// <summary>
/// A fixed page of the site and app that the staff can edit: the terms, the privacy policy, about us,
/// cooperation, the rules and the questions people ask. The text is kept in three languages.
/// </summary>
public sealed class ContentPage : Entity
{
    public const string Terms = "terms";

    public const string Privacy = "privacy";

    public const string About = "about";

    public const string Cooperation = "cooperation";

    public const string Rules = "rules";

    public const string Faq = "faq";

    public const int MaximumTitleLength = 150;

    public const int MaximumBodyLength = 60000;

    /// <summary>The pages that exist, in the order they are listed.</summary>
    public static readonly IReadOnlyList<string> AllSlugs = [Terms, Privacy, About, Cooperation, Rules, Faq];

    private ContentPage()
    {
    }

    private ContentPage(Guid id, string slug)
        : base(id)
    {
        if (!AllSlugs.Contains(slug))
        {
            throw new DomainException("Page is unknown.");
        }

        Slug = slug;
    }

    public string Slug { get; private set; } = string.Empty;

    public string TitleAz { get; private set; } = string.Empty;

    public string TitleRu { get; private set; } = string.Empty;

    public string TitleEn { get; private set; } = string.Empty;

    public string BodyAz { get; private set; } = string.Empty;

    public string BodyRu { get; private set; } = string.Empty;

    public string BodyEn { get; private set; } = string.Empty;

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>The staff member who last edited it; empty for the text the site started with.</summary>
    public Guid? UpdatedByUserId { get; private set; }

    public static ContentPage Create(
        string slug,
        string titleAz, string titleRu, string titleEn,
        string bodyAz, string bodyRu, string bodyEn,
        DateTimeOffset nowUtc,
        Guid? updatedByUserId = null)
    {
        ContentPage page = new(Guid.NewGuid(), slug);
        page.Update(titleAz, titleRu, titleEn, bodyAz, bodyRu, bodyEn, nowUtc, updatedByUserId);
        return page;
    }

    public void Update(
        string titleAz, string titleRu, string titleEn,
        string bodyAz, string bodyRu, string bodyEn,
        DateTimeOffset nowUtc,
        Guid? updatedByUserId)
    {
        if (nowUtc == default || nowUtc.Offset != TimeSpan.Zero)
        {
            throw new DomainException("The time must be a UTC time.");
        }

        if (updatedByUserId == Guid.Empty)
        {
            throw new DomainException("The person who edited the page is invalid.");
        }

        string newTitleAz = RequireTitle(titleAz);
        string newTitleRu = RequireTitle(titleRu);
        string newTitleEn = RequireTitle(titleEn);
        string newBodyAz = RequireBody(bodyAz);
        string newBodyRu = RequireBody(bodyRu);
        string newBodyEn = RequireBody(bodyEn);

        TitleAz = newTitleAz;
        TitleRu = newTitleRu;
        TitleEn = newTitleEn;
        BodyAz = newBodyAz;
        BodyRu = newBodyRu;
        BodyEn = newBodyEn;
        UpdatedAtUtc = nowUtc;
        UpdatedByUserId = updatedByUserId;
    }

    public string TitleFor(PreferredLanguage language) =>
        language switch
        {
            PreferredLanguage.Russian => TitleRu,
            PreferredLanguage.English => TitleEn,
            _ => TitleAz
        };

    public string BodyFor(PreferredLanguage language) =>
        language switch
        {
            PreferredLanguage.Russian => BodyRu,
            PreferredLanguage.English => BodyEn,
            _ => BodyAz
        };

    private static string RequireTitle(string? value)
    {
        string text = (value ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            throw new DomainException("The page title is required in all three languages.");
        }

        if (text.Length > MaximumTitleLength)
        {
            throw new DomainException($"The page title cannot exceed {MaximumTitleLength} characters.");
        }

        return text;
    }

    private static string RequireBody(string? value)
    {
        string text = (value ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
        if (text.Length == 0)
        {
            throw new DomainException("The page text is required in all three languages.");
        }

        if (text.Length > MaximumBodyLength)
        {
            throw new DomainException($"The page text cannot exceed {MaximumBodyLength} characters.");
        }

        return text;
    }
}
