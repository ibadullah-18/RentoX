using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Content;
using RentoX.Application.Files;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Content;
using RentoX.Infrastructure.Files;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Content;

public sealed class BrandingService(
    RentoXDbContext dbContext,
    IFileStorage fileStorage,
    IClock clock,
    ICurrentUserContext currentUserContext)
    : IBrandingService
{
    public static string SlugOf(BrandingImageSlot slot) => slot switch
    {
        BrandingImageSlot.HeaderLogo => "header-logo",
        BrandingImageSlot.HeaderLogoDark => "header-logo-dark",
        BrandingImageSlot.FooterLogo => "footer-logo",
        BrandingImageSlot.FooterLogoDark => "footer-logo-dark",
        BrandingImageSlot.Icon => "icon",
        BrandingImageSlot.ShareImage => "share-image",
        _ => throw new DomainException("The picture is not known.")
    };

    public static bool TryParseSlot(string? slug, out BrandingImageSlot slot)
    {
        foreach (BrandingImageSlot candidate in Enum.GetValues<BrandingImageSlot>())
        {
            if (string.Equals(SlugOf(candidate), slug, StringComparison.OrdinalIgnoreCase))
            {
                slot = candidate;
                return true;
            }
        }

        slot = default;
        return false;
    }

    public async Task<BrandingPublic> GetAsync(CancellationToken cancellationToken = default)
    {
        SiteBranding? branding = await dbContext.Set<SiteBranding>().AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return Map(branding ?? SiteBranding.CreateDefault());
    }

    public async Task<BrandingPublic> UpdateAsync(BrandingValues values, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);
        Guid actor = RequireActor();

        SiteBranding branding = await LoadOrCreateAsync(cancellationToken);
        branding.Update(
            values.SiteName, values.FooterText, values.UseTextLogo, values.HeaderLogoHeight, values.FooterLogoHeight,
            values.MotionLevel, values.DefaultTheme, values.Description,
            values.AnnouncementEnabled, values.AnnouncementText, values.AnnouncementLink, values.AnnouncementTone,
            clock.UtcNow, actor);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(branding);
    }

    public async Task<BrandingPublic> SetImageAsync(
        BrandingImageSlot slot, Stream content, string fileName, string contentType, long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        Guid actor = RequireActor();

        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        string type = contentType.Trim().ToLowerInvariant();
        using MemoryStream validated = await ImageUploadValidator.ReadValidatedAsync(
            content, sizeBytes, type, extension, cancellationToken);

        StoredFileResult stored = await fileStorage.SaveAsync(
            FileStorageArea.Branding, validated, type, extension, cancellationToken);

        SiteBranding branding = await LoadOrCreateAsync(cancellationToken);
        string? previous;
        try
        {
            previous = branding.SetImage(slot, stored.StorageKey, clock.UtcNow, actor);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await fileStorage.DeleteAsync(stored.StorageKey, CancellationToken.None);
            throw;
        }

        if (previous is not null)
        {
            await fileStorage.DeleteAsync(previous, cancellationToken);
        }

        return Map(branding);
    }

    public async Task<BrandingPublic> ResetImageAsync(BrandingImageSlot slot, CancellationToken cancellationToken = default)
    {
        Guid actor = RequireActor();
        SiteBranding branding = await LoadOrCreateAsync(cancellationToken);
        string? previous = branding.SetImage(slot, null, clock.UtcNow, actor);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (previous is not null)
        {
            await fileStorage.DeleteAsync(previous, cancellationToken);
        }

        return Map(branding);
    }

    public async Task<BrandingImageContent?> OpenImageAsync(BrandingImageSlot slot, CancellationToken cancellationToken = default)
    {
        SiteBranding? branding = await dbContext.Set<SiteBranding>().AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        string? key = branding?.KeyOf(slot);
        if (key is null)
        {
            return null;
        }

        Stream? stream = await fileStorage.OpenReadAsync(key, cancellationToken);
        if (stream is null)
        {
            return null;
        }

        string contentType = Path.GetExtension(key).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };
        return new BrandingImageContent(stream, contentType);
    }

    private async Task<SiteBranding> LoadOrCreateAsync(CancellationToken cancellationToken)
    {
        SiteBranding? branding = await dbContext.Set<SiteBranding>().FirstOrDefaultAsync(cancellationToken);
        if (branding is null)
        {
            branding = SiteBranding.CreateDefault();
            dbContext.Set<SiteBranding>().Add(branding);
        }

        return branding;
    }

    private static BrandingPublic Map(SiteBranding b)
    {
        long version = b.UpdatedAtUtc == default ? 0 : b.UpdatedAtUtc.UtcTicks;
        string? Url(BrandingImageSlot slot) =>
            b.KeyOf(slot) is null ? null : $"/api/branding/{SlugOf(slot)}/image?v={version}";

        return new BrandingPublic(
            b.SiteName, b.FooterText, b.UseTextLogo, b.HeaderLogoHeight, b.FooterLogoHeight,
            Url(BrandingImageSlot.HeaderLogo), Url(BrandingImageSlot.HeaderLogoDark),
            Url(BrandingImageSlot.FooterLogo), Url(BrandingImageSlot.FooterLogoDark),
            Url(BrandingImageSlot.Icon), version,
            Url(BrandingImageSlot.ShareImage), b.MotionLevel, b.DefaultTheme, b.Description,
            b.AnnouncementEnabled && b.AnnouncementText is not null, b.AnnouncementText, b.AnnouncementLink,
            b.AnnouncementTone);
    }

    private Guid RequireActor()
    {
        Guid? id = currentUserContext.UserId;
        if (!currentUserContext.IsAuthenticated || !id.HasValue || id.Value == Guid.Empty)
        {
            throw new DomainException("An authenticated admin is required.");
        }

        return id.Value;
    }
}
