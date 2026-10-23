using RentoX.Domain.Content;

namespace RentoX.Application.Content;

/// <summary>What the owner can change as text and numbers (the pictures are changed one by one).</summary>
public sealed record BrandingValues(
    string SiteName,
    string? FooterText,
    bool UseTextLogo,
    int HeaderLogoHeight,
    int FooterLogoHeight,
    int MotionLevel = 0,
    int DefaultTheme = 0,
    string? Description = null,
    bool AnnouncementEnabled = false,
    string? AnnouncementText = null,
    string? AnnouncementLink = null,
    int AnnouncementTone = 0);

/// <summary>
/// What every visitor of the site and the apps gets. A picture address is null while the owner has not uploaded their
/// own: then the picture that comes with the site is used. [Version] changes with every edit, so a changed picture is
/// loaded again at once.
/// </summary>
public sealed record BrandingPublic(
    string SiteName,
    string? FooterText,
    bool UseTextLogo,
    int HeaderLogoHeight,
    int FooterLogoHeight,
    string? HeaderLogoUrl,
    string? HeaderLogoDarkUrl,
    string? FooterLogoUrl,
    string? FooterLogoDarkUrl,
    string? IconUrl,
    long Version,
    string? ShareImageUrl = null,
    int MotionLevel = 0,
    int DefaultTheme = 0,
    string? Description = null,
    bool AnnouncementEnabled = false,
    string? AnnouncementText = null,
    string? AnnouncementLink = null,
    int AnnouncementTone = 0);

public sealed record BrandingImageContent(Stream Content, string ContentType);

public interface IBrandingService
{
    Task<BrandingPublic> GetAsync(CancellationToken cancellationToken = default);

    Task<BrandingPublic> UpdateAsync(BrandingValues values, CancellationToken cancellationToken = default);

    /// <summary>Sets (or replaces) one picture. It is checked like any uploaded image.</summary>
    Task<BrandingPublic> SetImageAsync(
        BrandingImageSlot slot, Stream content, string fileName, string contentType, long sizeBytes,
        CancellationToken cancellationToken = default);

    /// <summary>Takes the owner's picture away: the one that comes with the site is shown again.</summary>
    Task<BrandingPublic> ResetImageAsync(BrandingImageSlot slot, CancellationToken cancellationToken = default);

    Task<BrandingImageContent?> OpenImageAsync(BrandingImageSlot slot, CancellationToken cancellationToken = default);
}
