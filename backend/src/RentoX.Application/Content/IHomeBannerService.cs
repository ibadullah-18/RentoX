using RentoX.Domain.Users.Enums;

namespace RentoX.Application.Content;

public sealed record BannerInput(
    string TitleAz, string TitleRu, string TitleEn,
    string? SubtitleAz, string? SubtitleRu, string? SubtitleEn,
    string? LinkUrl, int DisplayOrder, bool IsActive,
    DateTimeOffset? StartsAtUtc, DateTimeOffset? EndsAtUtc,
    string? PageSlug = null,
    string? PageBodyAz = null, string? PageBodyRu = null, string? PageBodyEn = null,
    string? PageButtonLabelAz = null, string? PageButtonLabelRu = null, string? PageButtonLabelEn = null,
    string? PageButtonUrl = null,
    bool OpenInNewTab = false,
    int Platform = 3,
    bool ShowText = true,
    int TextAlign = 0,
    bool DarkOverlay = true,
    int TextTone = 0);

public sealed record AdminBannerItem(
    Guid Id,
    string TitleAz, string TitleRu, string TitleEn,
    string? SubtitleAz, string? SubtitleRu, string? SubtitleEn,
    string? LinkUrl, int DisplayOrder, bool IsActive,
    DateTimeOffset? StartsAtUtc, DateTimeOffset? EndsAtUtc,
    bool HasImage, bool IsLive,
    string? PageSlug = null,
    string? PageBodyAz = null, string? PageBodyRu = null, string? PageBodyEn = null,
    string? PageButtonLabelAz = null, string? PageButtonLabelRu = null, string? PageButtonLabelEn = null,
    string? PageButtonUrl = null,
    bool OpenInNewTab = false,
    int Platform = 3,
    bool ShowText = true,
    int TextAlign = 0,
    bool DarkOverlay = true,
    int TextTone = 0);

public sealed record PublicBannerItem(
    Guid Id, string Title, string? Subtitle, string ImageUrl, string? LinkUrl,
    string? PageSlug = null,
    bool OpenInNewTab = false,
    bool ShowText = true,
    int TextAlign = 0,
    bool DarkOverlay = true,
    int TextTone = 0);

/// <summary>The page of a campaign (rentox.az/slug).</summary>
/// <param name="IsOver">The campaign has ended: the page says so instead of promising anything.</param>
public sealed record CampaignPageItem(
    string Slug, string Title, string? Subtitle, string? Body, string ImageUrl,
    string? ButtonLabel, string? ButtonUrl, bool IsOver);

public sealed record BannerImageContent(Stream Content, string ContentType);

/// <summary>The banners of the home page: managed by the staff, shown to everyone.</summary>
public interface IHomeBannerService
{
    Task<IReadOnlyList<AdminBannerItem>> ListAsync(CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(BannerInput input, CancellationToken cancellationToken = default);

    Task UpdateAsync(Guid id, BannerInput input, CancellationToken cancellationToken = default);

    /// <summary>Sets (or replaces) the picture of a banner. The picture is checked like any uploaded image.</summary>
    Task SetImageAsync(
        Guid id, Stream content, string fileName, string contentType, long sizeBytes,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// The banners on show right now, in order, in the given language. [platform]: 1 website, 2 apps (a banner made
    /// for one place is not shown in the other).
    /// </summary>
    Task<IReadOnlyList<PublicBannerItem>> GetLiveAsync(
        PreferredLanguage language, int platform = 3, CancellationToken cancellationToken = default);

    /// <summary>The page of a campaign; null when there is none by that name, or it is switched off or not started.</summary>
    Task<CampaignPageItem?> GetPageAsync(
        string slug, PreferredLanguage language, CancellationToken cancellationToken = default);

    Task<BannerImageContent?> OpenImageAsync(Guid id, CancellationToken cancellationToken = default);
}
