using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Content;
using RentoX.Application.Files;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Content;
using RentoX.Infrastructure.Content;
using RentoX.Infrastructure.Persistence;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Content;

/// <summary>The name, logos and icon the owner can change: defaults, edits, replaced pictures and refused ones.</summary>
public sealed class BrandingTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 9, 0, 0, TimeSpan.Zero);

    // A real 1x1 PNG: the check of the picture reads it.
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

    [Fact]
    public async Task WithNothingSetTheSiteKeepsItsNameAndUsesThePicturesThatComeWithIt()
    {
        MemoryStorage storage = new();
        await using RentoXDbContext db = database.CreateContext();
        await db.Set<SiteBranding>().ExecuteDeleteAsync();

        BrandingPublic branding = await Service(db, storage).GetAsync();

        Assert.Equal("RentoX", branding.SiteName);
        Assert.False(branding.UseTextLogo);
        Assert.Equal(40, branding.HeaderLogoHeight);
        Assert.Null(branding.HeaderLogoUrl);
        Assert.Null(branding.IconUrl);
        Assert.Equal(0, branding.Version);
    }

    [Fact]
    public async Task TheNameTheFooterTextAndTheSizesAreSavedAndWrongOnesAreRefused()
    {
        MemoryStorage storage = new();
        await using RentoXDbContext db = database.CreateContext();
        await db.Set<SiteBranding>().ExecuteDeleteAsync();
        BrandingService service = Service(db, storage);

        BrandingPublic saved = await service.UpdateAsync(new BrandingValues("Rentox.az", "  © 2026 Rentox  ", true, 48, 56));
        Assert.Equal("Rentox.az", saved.SiteName);
        Assert.Equal("© 2026 Rentox", saved.FooterText);
        Assert.True(saved.UseTextLogo);
        Assert.Equal(48, saved.HeaderLogoHeight);
        Assert.Equal(56, saved.FooterLogoHeight);
        Assert.True(saved.Version > 0);

        await Assert.ThrowsAsync<DomainException>(() => service.UpdateAsync(new BrandingValues("", null, false, 40, 40)));
        await Assert.ThrowsAsync<DomainException>(() => service.UpdateAsync(new BrandingValues(new string('x', 41), null, false, 40, 40)));
        await Assert.ThrowsAsync<DomainException>(() => service.UpdateAsync(new BrandingValues("Rentox", null, false, 5, 40)));
        await Assert.ThrowsAsync<DomainException>(() => service.UpdateAsync(new BrandingValues("Rentox", null, false, 40, 400)));

        // A refused edit changed nothing.
        BrandingPublic again = await service.GetAsync();
        Assert.Equal("Rentox.az", again.SiteName);
        Assert.Equal(48, again.HeaderLogoHeight);
    }

    [Fact]
    public async Task TheLookTheMotionTheDescriptionAndTheNoticeAreSavedAndWrongOnesRefused()
    {
        MemoryStorage storage = new();
        await using RentoXDbContext db = database.CreateContext();
        await db.Set<SiteBranding>().ExecuteDeleteAsync();
        BrandingService service = Service(db, storage);

        BrandingPublic start = await service.GetAsync();
        Assert.Equal(0, start.MotionLevel);
        Assert.Equal(0, start.DefaultTheme);
        Assert.False(start.AnnouncementEnabled);

        BrandingPublic saved = await service.UpdateAsync(new BrandingValues(
            "RentoX", null, false, 40, 40, 1, 2, "  Kirayə elanları  ",
            true, "Texniki işlər 02:00-da", "/destek", 1));
        Assert.Equal(1, saved.MotionLevel);
        Assert.Equal(2, saved.DefaultTheme);
        Assert.Equal("Kirayə elanları", saved.Description);
        Assert.True(saved.AnnouncementEnabled);
        Assert.Equal("Texniki işlər 02:00-da", saved.AnnouncementText);
        Assert.Equal("/destek", saved.AnnouncementLink);
        Assert.Equal(1, saved.AnnouncementTone);

        // A notice that is switched on needs words; a link must be a place of the site or an http(s) address.
        await Assert.ThrowsAsync<DomainException>(() => service.UpdateAsync(new BrandingValues(
            "RentoX", null, false, 40, 40, 0, 0, null, true, " ", null, 0)));
        await Assert.ThrowsAsync<DomainException>(() => service.UpdateAsync(new BrandingValues(
            "RentoX", null, false, 40, 40, 0, 0, null, true, "Xəbər", "javascript:alert(1)", 0)));
        await Assert.ThrowsAsync<DomainException>(() => service.UpdateAsync(new BrandingValues(
            "RentoX", null, false, 40, 40, 0, 0, null, true, "Xəbər", "//evil.example", 0)));
        await Assert.ThrowsAsync<DomainException>(() => service.UpdateAsync(new BrandingValues(
            "RentoX", null, false, 40, 40, 3, 0, null, false, null, null, 0)));
        await Assert.ThrowsAsync<DomainException>(() => service.UpdateAsync(new BrandingValues(
            "RentoX", null, false, 40, 40, 0, 0, new string('a', 301), false, null, null, 0)));

        // A notice with text but switched off is kept, and not shown.
        BrandingPublic off = await service.UpdateAsync(new BrandingValues(
            "RentoX", null, false, 40, 40, 0, 0, null, false, "Köhnə xəbər", "https://rentox.az/x", 2));
        Assert.False(off.AnnouncementEnabled);
        Assert.Equal("Köhnə xəbər", off.AnnouncementText);

        // The picture for shared links has its own place.
        BrandingPublic withImage = await service.SetImageAsync(
            BrandingImageSlot.ShareImage, new MemoryStream(Png), "share.png", "image/png", Png.Length);
        Assert.Contains("/api/branding/share-image/image?v=", withImage.ShareImageUrl, StringComparison.Ordinal);
        Assert.True(BrandingService.TryParseSlot("share-image", out BrandingImageSlot slot));
        Assert.Equal(BrandingImageSlot.ShareImage, slot);
    }

    [Fact]
    public async Task APictureIsServedReplacedAndTakenAwayAndEveryOldFileIsDeleted()
    {
        MemoryStorage storage = new();
        await using RentoXDbContext db = database.CreateContext();
        await db.Set<SiteBranding>().ExecuteDeleteAsync();
        BrandingService service = Service(db, storage);

        BrandingPublic first = await service.SetImageAsync(
            BrandingImageSlot.HeaderLogo, new MemoryStream(Png), "logo.png", "image/png", Png.Length);
        Assert.NotNull(first.HeaderLogoUrl);
        Assert.Contains("/api/branding/header-logo/image?v=", first.HeaderLogoUrl, StringComparison.Ordinal);
        Assert.Null(first.IconUrl);
        Assert.Single(storage.Files);

        BrandingImageContent? served = await service.OpenImageAsync(BrandingImageSlot.HeaderLogo);
        Assert.NotNull(served);
        Assert.Equal("image/png", served.ContentType);
        Assert.Null(await service.OpenImageAsync(BrandingImageSlot.Icon));

        // A new picture replaces the old one, and its file is gone.
        await service.SetImageAsync(BrandingImageSlot.HeaderLogo, new MemoryStream(Png), "logo2.png", "image/png", Png.Length);
        Assert.Single(storage.Files);

        await service.SetImageAsync(BrandingImageSlot.Icon, new MemoryStream(Png), "icon.png", "image/png", Png.Length);
        Assert.Equal(2, storage.Files.Count);

        BrandingPublic reset = await service.ResetImageAsync(BrandingImageSlot.HeaderLogo);
        Assert.Null(reset.HeaderLogoUrl);
        Assert.NotNull(reset.IconUrl);
        Assert.Single(storage.Files);
    }

    [Fact]
    public async Task AFileThatIsNotAPictureIsRefusedAndNothingIsStored()
    {
        MemoryStorage storage = new();
        await using RentoXDbContext db = database.CreateContext();
        await db.Set<SiteBranding>().ExecuteDeleteAsync();
        BrandingService service = Service(db, storage);

        byte[] text = "not a picture at all"u8.ToArray();
        await Assert.ThrowsAsync<DomainException>(() => service.SetImageAsync(
            BrandingImageSlot.FooterLogo, new MemoryStream(text), "logo.png", "image/png", text.Length));
        await Assert.ThrowsAsync<DomainException>(() => service.SetImageAsync(
            BrandingImageSlot.FooterLogo, new MemoryStream(Png), "logo.gif", "image/gif", Png.Length));

        Assert.Empty(storage.Files);
        Assert.Null((await service.GetAsync()).FooterLogoUrl);
    }

    [Fact]
    public void ThePictureNamesInAddressesAreTheOnesTheSiteKnows()
    {
        Assert.True(BrandingService.TryParseSlot("header-logo-dark", out BrandingImageSlot slot));
        Assert.Equal(BrandingImageSlot.HeaderLogoDark, slot);
        Assert.True(BrandingService.TryParseSlot("ICON", out _));
        Assert.False(BrandingService.TryParseSlot("favicon", out _));
        Assert.False(BrandingService.TryParseSlot("../etc/passwd", out _));
    }

    private static BrandingService Service(RentoXDbContext db, MemoryStorage storage) =>
        new(db, storage, new Clock(), new Actor(Guid.NewGuid()));

    private sealed class Clock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class Actor(Guid userId) : ICurrentUserContext
    {
        public Guid? UserId => userId;

        public bool IsAuthenticated => true;
    }

    private sealed class MemoryStorage : IFileStorage
    {
        public Dictionary<string, byte[]> Files { get; } = [];

        public async Task<StoredFileResult> SaveAsync(
            FileStorageArea area, Stream content, string contentType, string extension, CancellationToken cancellationToken = default)
        {
            using MemoryStream copy = new();
            await content.CopyToAsync(copy, cancellationToken);
            string key = $"branding/{Guid.NewGuid():N}{extension}";
            Files[key] = copy.ToArray();
            return new StoredFileResult(key, contentType, copy.Length);
        }

        public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream?>(Files.TryGetValue(storageKey, out byte[]? data) ? new MemoryStream(data) : null);

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            Files.Remove(storageKey);
            return Task.CompletedTask;
        }
    }
}
