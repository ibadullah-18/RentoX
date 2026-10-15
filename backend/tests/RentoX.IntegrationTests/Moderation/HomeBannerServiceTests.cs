using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Content;
using RentoX.Application.Files;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Content;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Content;
using RentoX.Infrastructure.Persistence;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Moderation;

/// <summary>The home banners: who sees which, in which language, and what happens to the picture.</summary>
public sealed class HomeBannerServiceTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

    // A real 1x1 PNG, so the upload check accepts it.
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private static BannerInput Input(string title, int order = 1, bool active = true,
        DateTimeOffset? starts = null, DateTimeOffset? ends = null, string? link = "/destek") =>
        new(title, title + " RU", title + " EN", "Alt yazı", null, null, link, order, active, starts, ends);

    [Fact]
    public async Task OnlyBannersWithAPictureThatAreOnAndInsideTheirDatesAreShownInOrder()
    {
        await ResetAsync();
        var files = new MemoryFiles();
        Guid late = await Create(files, "Sonra", order: 2, withImage: true);
        Guid early = await Create(files, "Əvvəl", order: 1, withImage: true);
        await Create(files, "Şəkilsiz", order: 0, withImage: false);
        await Create(files, "Söndürülüb", order: 0, withImage: true, active: false);
        await Create(files, "Gələcəkdə", order: 0, withImage: true, starts: Now.AddDays(2));
        await Create(files, "Bitib", order: 0, withImage: true, ends: Now.AddDays(-1), starts: Now.AddDays(-5));

        var shown = await Service(files).GetLiveAsync(PreferredLanguage.Azerbaijani);

        Assert.Equal(new[] { early, late }, shown.Select(b => b.Id).ToArray());
        Assert.Equal($"/api/banners/{early:D}/image", shown[0].ImageUrl);
        Assert.Equal("/destek", shown[0].LinkUrl);
    }

    [Fact]
    public async Task TheTextIsGivenInTheChosenLanguage()
    {
        await ResetAsync();
        var files = new MemoryFiles();
        await Create(files, "Salam", order: 1, withImage: true);

        Assert.Equal("Salam", (await Service(files).GetLiveAsync(PreferredLanguage.Azerbaijani)).Single().Title);
        Assert.Equal("Salam RU", (await Service(files).GetLiveAsync(PreferredLanguage.Russian)).Single().Title);
        Assert.Equal("Salam EN", (await Service(files).GetLiveAsync(PreferredLanguage.English)).Single().Title);
    }

    [Fact]
    public async Task ReplacingThePictureRemovesTheOldFileAndDeletingTheBannerRemovesThePicture()
    {
        await ResetAsync();
        var files = new MemoryFiles();
        Guid id = await Create(files, "Şəkil", order: 1, withImage: true);
        string first = files.Keys.Single();

        await Service(files).SetImageAsync(id, new MemoryStream(Png), "yeni.png", "image/png", Png.Length);
        Assert.DoesNotContain(first, files.Keys);
        Assert.Single(files.Keys);

        Assert.NotNull(await Service(files).OpenImageAsync(id));

        Assert.True(await Service(files).DeleteAsync(id));
        Assert.Empty(files.Keys);
        Assert.False(await Service(files).DeleteAsync(id));
        Assert.Null(await Service(files).OpenImageAsync(id));
    }

    [Fact]
    public async Task ABadPictureIsRefusedAndTheBannerKeepsItsOldOne()
    {
        await ResetAsync();
        var files = new MemoryFiles();
        Guid id = await Create(files, "Şəkil", order: 1, withImage: true);
        string before = files.Keys.Single();

        byte[] notAnImage = "this is not a picture"u8.ToArray();
        await Assert.ThrowsAnyAsync<Exception>(() => Service(files).SetImageAsync(
            id, new MemoryStream(notAnImage), "x.png", "image/png", notAnImage.Length));

        Assert.Equal(before, files.Keys.Single());
        await Assert.ThrowsAsync<DomainException>(() => Service(files).SetImageAsync(
            Guid.NewGuid(), new MemoryStream(Png), "x.png", "image/png", Png.Length));
    }

    [Fact]
    public async Task TheAdminListShowsEverythingWithItsState()
    {
        await ResetAsync();
        var files = new MemoryFiles();
        await Create(files, "Canlı", order: 1, withImage: true);
        await Create(files, "Şəkilsiz", order: 2, withImage: false);

        var all = await Service(files).ListAsync();

        Assert.Equal(2, all.Count);
        Assert.True(all[0].IsLive);
        Assert.True(all[0].HasImage);
        Assert.False(all[1].IsLive);
        Assert.False(all[1].HasImage);
    }

    [Fact]
    public async Task OnlyASignedInAdminCanCreate()
    {
        await ResetAsync();
        var anonymous = new HomeBannerService(database.CreateContext(), new MemoryFiles(), new TestClock(), new TestActor(null));

        await Assert.ThrowsAsync<DomainException>(() => anonymous.CreateAsync(Input("Qadağan")));
    }

    private async Task<Guid> Create(MemoryFiles files, string title, int order, bool withImage,
        bool active = true, DateTimeOffset? starts = null, DateTimeOffset? ends = null)
    {
        Guid id = await Service(files).CreateAsync(Input(title, order, active, starts, ends));
        if (withImage)
        {
            await Service(files).SetImageAsync(id, new MemoryStream(Png), "a.png", "image/png", Png.Length);
        }

        return id;
    }

    private async Task ResetAsync()
    {
        await using RentoXDbContext db = database.CreateContext();
        await db.Set<HomeBanner>().ExecuteDeleteAsync();
    }

    private HomeBannerService Service(MemoryFiles files) =>
        new(database.CreateContext(), files, new TestClock(), new TestActor(Guid.NewGuid()));

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class TestActor(Guid? userId) : ICurrentUserContext
    {
        public Guid? UserId => userId;

        public bool IsAuthenticated => userId is not null;
    }

    /// <summary>Pictures kept in memory, so the test can see which files exist.</summary>
    private sealed class MemoryFiles : IFileStorage
    {
        private readonly Dictionary<string, byte[]> store = [];

        public IReadOnlyCollection<string> Keys => store.Keys;

        public async Task<StoredFileResult> SaveAsync(
            FileStorageArea area, Stream content, string contentType, string extension,
            CancellationToken cancellationToken = default)
        {
            using MemoryStream copy = new();
            await content.CopyToAsync(copy, cancellationToken);
            string key = $"banners/{Guid.NewGuid():N}{extension}";
            store[key] = copy.ToArray();
            return new StoredFileResult(key, contentType, copy.Length);
        }

        public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream?>(store.TryGetValue(storageKey, out byte[]? bytes) ? new MemoryStream(bytes) : null);

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            store.Remove(storageKey);
            return Task.CompletedTask;
        }
    }
}
