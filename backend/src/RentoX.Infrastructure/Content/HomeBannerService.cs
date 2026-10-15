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
            b.ImageKey is not null, b.IsLive(now))).ToArray();
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
        await dbContext.SaveChangesAsync(cancellationToken);
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
        PreferredLanguage language, CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = clock.UtcNow;
        List<HomeBanner> banners = await dbContext.Set<HomeBanner>().AsNoTracking()
            .Where(b => b.IsActive && b.ImageKey != null)
            .OrderBy(b => b.DisplayOrder).ThenBy(b => b.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return banners
            .Where(b => b.IsLive(now))
            .Select(b => new PublicBannerItem(
                b.Id, b.TitleFor(language), b.SubtitleFor(language), $"/api/banners/{b.Id:D}/image", b.LinkUrl))
            .ToArray();
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
