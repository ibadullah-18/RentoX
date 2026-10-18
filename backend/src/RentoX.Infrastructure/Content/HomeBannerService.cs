using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Content;
using RentoX.Application.Files;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Content;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Files;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Content;

public sealed class HomeBannerService(
    RentoXDbContext dbContext,
    IFileStorage fileStorage,
    IClock clock,
    ICurrentUserContext currentUserContext)
    : IHomeBannerService
{
    private const int MaximumBanners = 30;

    public async Task<IReadOnlyList<AdminBannerItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = clock.UtcNow;
        List<HomeBanner> banners = await dbContext.Set<HomeBanner>().AsNoTracking()
            .OrderBy(b => b.DisplayOrder).ThenBy(b => b.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return banners.Select(b => new AdminBannerItem(
            b.Id, b.TitleAz, b.TitleRu, b.TitleEn, b.SubtitleAz, b.SubtitleRu, b.SubtitleEn,
            b.LinkUrl, b.DisplayOrder, b.IsActive, b.StartsAtUtc, b.EndsAtUtc,
            b.ImageKey is not null, b.IsLive(now),
            b.PageSlug, b.PageBodyAz, b.PageBodyRu, b.PageBodyEn,
            b.PageButtonLabelAz, b.PageButtonLabelRu, b.PageButtonLabelEn, b.PageButtonUrl,
            b.OpenInNewTab, b.Platform, b.ShowText, b.TextAlign, b.DarkOverlay, b.TextTone)).ToArray();
    }

    public async Task<Guid> CreateAsync(BannerInput input, CancellationToken cancellationToken = default)
    {
        Guid actor = RequireActor();
        if (await dbContext.Set<HomeBanner>().CountAsync(cancellationToken) >= MaximumBanners)
        {
            throw new DomainException($"There cannot be more than {MaximumBanners} banners. Delete old ones.");
        }

        HomeBanner banner = HomeBanner.Create(
            input.TitleAz, input.TitleRu, input.TitleEn,
            input.SubtitleAz, input.SubtitleRu, input.SubtitleEn,
            input.LinkUrl, input.DisplayOrder, input.IsActive,
            input.StartsAtUtc?.ToUniversalTime(), input.EndsAtUtc?.ToUniversalTime(),
            actor, clock.UtcNow);
        banner.SetCampaign(CampaignOf(input));
        await EnsureSlugFreeAsync(banner.PageSlug, banner.Id, cancellationToken);

        dbContext.Set<HomeBanner>().Add(banner);
        await dbContext.SaveChangesAsync(cancellationToken);
        return banner.Id;
    }

    public async Task UpdateAsync(Guid id, BannerInput input, CancellationToken cancellationToken = default)
    {
        HomeBanner banner = await dbContext.Set<HomeBanner>().SingleOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new DomainException("Banner was not found.");

        banner.Update(
            input.TitleAz, input.TitleRu, input.TitleEn,
            input.SubtitleAz, input.SubtitleRu, input.SubtitleEn,
            input.LinkUrl, input.DisplayOrder, input.IsActive,
            input.StartsAtUtc?.ToUniversalTime(), input.EndsAtUtc?.ToUniversalTime());
        banner.SetCampaign(CampaignOf(input));
        await EnsureSlugFreeAsync(banner.PageSlug, banner.Id, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static BannerCampaign CampaignOf(BannerInput input) => new(
        input.PageSlug,
        input.PageBodyAz, input.PageBodyRu, input.PageBodyEn,
        input.PageButtonLabelAz, input.PageButtonLabelRu, input.PageButtonLabelEn,
        input.PageButtonUrl,
        input.OpenInNewTab, input.Platform, input.ShowText, input.TextAlign, input.DarkOverlay, input.TextTone);

    private async Task EnsureSlugFreeAsync(string? slug, Guid bannerId, CancellationToken cancellationToken)
    {
        if (slug is null)
        {
            return;
        }

        if (await dbContext.Set<HomeBanner>().AnyAsync(b => b.PageSlug == slug && b.Id != bannerId, cancellationToken))
        {
            throw new DomainException("Another campaign already has this page name.");
        }
    }

    public async Task SetImageAsync(
        Guid id, Stream content, string fileName, string contentType, long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        HomeBanner banner = await dbContext.Set<HomeBanner>().SingleOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new DomainException("Banner was not found.");

        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        string type = contentType.Trim().ToLowerInvariant();

        using MemoryStream validated = await ImageUploadValidator.ReadValidatedAsync(
            content, sizeBytes, type, extension, cancellationToken);

        StoredFileResult stored = await fileStorage.SaveAsync(
            FileStorageArea.Banners, validated, type, extension, cancellationToken);

        string? previous = banner.ImageKey;
        try
        {
            banner.SetImage(stored.StorageKey);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await fileStorage.DeleteAsync(stored.StorageKey, cancellationToken);
            throw;
        }

        if (previous is not null)
        {
            await fileStorage.DeleteAsync(previous, cancellationToken);
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        HomeBanner? banner = await dbContext.Set<HomeBanner>().SingleOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (banner is null)
        {
            return false;
        }

        string? key = banner.ImageKey;
        dbContext.Set<HomeBanner>().Remove(banner);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (key is not null)
        {
            await fileStorage.DeleteAsync(key, cancellationToken);
        }

        return true;
    }

    public async Task<IReadOnlyList<PublicBannerItem>> GetLiveAsync(
        PreferredLanguage language, int platform = 3, CancellationToken cancellationToken = default)
    {
        platform = platform is 1 or 2 ? platform : 3;
        DateTimeOffset now = clock.UtcNow;
        List<HomeBanner> banners = await dbContext.Set<HomeBanner>().AsNoTracking()
            .Where(b => b.IsActive && b.ImageKey != null)
            .OrderBy(b => b.DisplayOrder).ThenBy(b => b.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return banners
            .Where(b => b.IsLive(now) && (platform == 3 || (b.Platform & platform) != 0))
            .Select(b => new PublicBannerItem(
                b.Id, b.TitleFor(language), b.SubtitleFor(language), $"/api/banners/{b.Id:D}/image", b.LinkUrl,
                b.PageSlug, b.OpenInNewTab, b.ShowText, b.TextAlign, b.DarkOverlay, b.TextTone))
            .ToArray();
    }

    public async Task<CampaignPageItem?> GetPageAsync(
        string slug, PreferredLanguage language, CancellationToken cancellationToken = default)
    {
        string name = (slug ?? string.Empty).Trim().ToLowerInvariant();
        if (name.Length == 0)
        {
            return null;
        }

        DateTimeOffset now = clock.UtcNow;
        HomeBanner? banner = await dbContext.Set<HomeBanner>().AsNoTracking()
            .SingleOrDefaultAsync(b => b.PageSlug == name, cancellationToken);

        // Switched off, without a picture, or not started yet: as if it did not exist.
        if (banner is null || !banner.IsActive || banner.ImageKey is null ||
            (banner.StartsAtUtc.HasValue && banner.StartsAtUtc.Value > now))
        {
            return null;
        }

        bool over = banner.EndsAtUtc.HasValue && banner.EndsAtUtc.Value <= now;
        string? buttonLabel = banner.PageButtonLabelFor(language);
        return new CampaignPageItem(
            name,
            banner.TitleFor(language),
            banner.SubtitleFor(language),
            banner.PageBodyFor(language),
            $"/api/banners/{banner.Id:D}/image",
            banner.PageButtonUrl is null ? null : buttonLabel,
            banner.PageButtonUrl,
            over);
    }

    public async Task<BannerImageContent?> OpenImageAsync(Guid id, CancellationToken cancellationToken = default)
    {
        string? key = await dbContext.Set<HomeBanner>().AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => b.ImageKey)
            .SingleOrDefaultAsync(cancellationToken);
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
        return new BannerImageContent(stream, contentType);
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
