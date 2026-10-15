using System.Text.RegularExpressions;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Users.Enums;

namespace RentoX.Application.Content;

public sealed record PublicPageSummary(string Slug, string Title);

public sealed record PublicPageResult(string Slug, string Title, string Body, DateTimeOffset UpdatedAtUtc);

public sealed record AdminPageItem(
    string Slug,
    string TitleAz, string TitleRu, string TitleEn,
    string BodyAz, string BodyRu, string BodyEn,
    DateTimeOffset UpdatedAtUtc);

public sealed record PageInput(
    string TitleAz, string TitleRu, string TitleEn,
    string BodyAz, string BodyRu, string BodyEn);

/// <summary>The operator's details and contact channels. An empty value is not shown anywhere.</summary>
public sealed record SiteInfoValues(
    string? OperatorName,
    string? Voen,
    string? Address,
    string? Email,
    string? Phone,
    string? Hotline,
    string? WhatsApp,
    string? Instagram,
    string? TikTok,
    string? Facebook);

/// <summary>The terms, privacy policy, about us and the other fixed pages, edited by the staff.</summary>
public interface IContentPageService
{
    Task<IReadOnlyList<PublicPageSummary>> ListPublicAsync(
        PreferredLanguage language, CancellationToken cancellationToken = default);

    /// <summary>One page for the public, in the given language, with the operator's details filled in.</summary>
    Task<PublicPageResult?> GetPublicAsync(
        string slug, PreferredLanguage language, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminPageItem>> ListAdminAsync(CancellationToken cancellationToken = default);

    Task UpdateAsync(string slug, PageInput input, CancellationToken cancellationToken = default);
}

public interface ISiteInfoService
{
    Task<SiteInfoValues> GetAsync(CancellationToken cancellationToken = default);

    Task<SiteInfoValues> UpdateAsync(SiteInfoValues values, CancellationToken cancellationToken = default);
}

/// <summary>
/// The marks a page text may contain, such as <c>{{email}}</c>, are replaced with the operator's
/// details when the page is shown. A line that is only a label and one mark ("Phone: {{phone}}")
/// disappears when that detail is empty, so no empty label is left behind.
/// </summary>
public static partial class PagePlaceholders
{
    public static readonly IReadOnlyList<string> Known =
        ["operatorName", "voen", "address", "email", "phone", "hotline"];

    [GeneratedRegex(@"\{\{(\w+)\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex Mark();

    [GeneratedRegex(@"^[^{}]*\{\{(\w+)\}\}\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex LabelLine();

    /// <summary>Throws when the text uses a mark that does not exist (a typo would show up on the live page).</summary>
    public static void Validate(string text)
    {
        foreach (Match match in Mark().Matches(text))
        {
            if (!Known.Contains(match.Groups[1].Value))
            {
                throw new DomainException(
                    $"The mark {{{{{match.Groups[1].Value}}}}} is unknown. Known marks: {string.Join(", ", Known.Select(k => "{{" + k + "}}"))}.");
            }
        }
    }

    public static string Resolve(string text, SiteInfoValues info)
    {
        string Value(string name) => name switch
        {
            "operatorName" => info.OperatorName ?? string.Empty,
            "voen" => info.Voen ?? string.Empty,
            "address" => info.Address ?? string.Empty,
            "email" => info.Email ?? string.Empty,
            "phone" => info.Phone ?? string.Empty,
            "hotline" => info.Hotline ?? string.Empty,
            _ => string.Empty
        };

        List<string> kept = [];
        foreach (string line in text.Split('\n'))
        {
            Match only = LabelLine().Match(line);
            if (only.Success && string.IsNullOrWhiteSpace(Value(only.Groups[1].Value)))
            {
                continue;
            }

            kept.Add(Mark().Replace(line, m =>
            {
                string value = Value(m.Groups[1].Value);
                return string.IsNullOrWhiteSpace(value) ? "—" : value;
            }));
        }

        return string.Join('\n', kept);
    }
}
