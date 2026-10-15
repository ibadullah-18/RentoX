using RentoX.Domain.Users.Enums;

namespace RentoX.Application.Content;

public sealed record BannerInput(
    string TitleAz, string TitleRu, string TitleEn,
    string? SubtitleAz, string? SubtitleRu, string? SubtitleEn,
    string? LinkUrl, int DisplayOrder, bool IsActive,
    DateTimeOffset? StartsAtUtc, DateTimeOffset? EndsAtUtc);

public sealed record AdminBannerItem(
    Guid Id,
    string TitleAz, string TitleRu, string TitleEn,
    string? SubtitleAz, string? SubtitleRu, string? SubtitleEn,
    string? LinkUrl, int DisplayOrder, bool IsActive,
    DateTimeOffset? StartsAtUtc, DateTimeOffset? EndsAtUtc,
    bool HasImage, bool IsLive);

public sealed record PublicBannerItem(Guid Id, string Title, string? Subtitle, string ImageUrl, string? LinkUrl);

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

    /// <summary>The banners on show right now, in order, in the given language.</summary>
    Task<IReadOnlyList<PublicBannerItem>> GetLiveAsync(
        PreferredLanguage language, CancellationToken cancellationToken = default);

    Task<BannerImageContent?> OpenImageAsync(Guid id, CancellationToken cancellationToken = default);
}
