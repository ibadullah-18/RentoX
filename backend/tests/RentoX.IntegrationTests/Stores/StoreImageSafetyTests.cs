using System.Text;
using Microsoft.EntityFrameworkCore;
using RentoX.Application.Files;
using RentoX.Application.Stores;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Stores;
using RentoX.Domain.Stores.Enums;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Persistence;
using RentoX.Infrastructure.Stores;
using RentoX.IntegrationTests.Auditing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace RentoX.IntegrationTests.Stores;

public sealed class StoreImageSafetyTests(
    AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    // Viewer: 0 = anonymous, 1 = owner, 2 = another user.
    [Theory]
    [InlineData(1, 0, false, false, false)]
    [InlineData(2, 0, false, false, false)]
    [InlineData(3, 0, false, false, true)]
    [InlineData(4, 0, false, false, false)]
    [InlineData(5, 0, false, false, false)]
    [InlineData(6, 0, false, false, false)]
    [InlineData(1, 1, false, false, true)]
    [InlineData(2, 1, false, false, true)]
    [InlineData(4, 1, false, false, true)]
    [InlineData(5, 1, false, false, true)]
    [InlineData(1, 2, false, false, false)]
    [InlineData(1, 2, true, false, true)]
    [InlineData(5, 2, true, false, true)]
    [InlineData(6, 1, false, false, false)]
    [InlineData(6, 2, true, false, false)]
    [InlineData(1, 0, true, false, false)]
    [InlineData(3, 1, false, true, false)]
    [InlineData(3, 2, true, true, false)]
    [InlineData(3, 2, false, false, true)]
    public async Task ImageAccessFollowsStoreVisibility(
        int status,
        int viewer,
        bool canModerate,
        bool softDeleted,
        bool expected)
    {
        await using RentoXDbContext context = database.CreateContext();

        StoreProfile store = await CreateStoreAsync(
            context,
            (StoreStatus)status,
            softDeleted);

        Guid? viewerId = viewer switch
        {
            1 => store.OwnerId,
            2 => Guid.NewGuid(),
            _ => null
        };

        StorageSpy storage = new();
        StoreImageService service = new(context, storage);

        StoreImageContentResult? result = await service.OpenAsync(
            store.Id,
            StoreImageKind.Logo,
            viewerId,
            canModerate);

        using Stream? content = result?.Content;

        Assert.Equal(expected, result is not null);
        Assert.Equal(expected ? 1 : 0, storage.OpenCount);

        if (expected)
        {
            Assert.Equal(store.LogoImageKey, storage.LastOpenedKey);
            Assert.Equal("image/png", result!.ContentType);
        }
    }

    [Theory]
    [InlineData(StoreImageKind.Logo)]
    [InlineData(StoreImageKind.Cover)]
    public async Task ExistingOverloadOnlyExposesActiveStores(
        StoreImageKind kind)
    {
        await using RentoXDbContext context = database.CreateContext();
        StoreProfile store = await CreateStoreAsync(
            context,
            StoreStatus.Draft,
            false);

        StorageSpy storage = new();
        StoreImageService service = new(context, storage);

        StoreImageContentResult? hidden = await service.OpenAsync(
            store.Id,
            kind);

        using Stream? hiddenContent = hidden?.Content;

        Assert.Null(hidden);
        Assert.Equal(0, storage.OpenCount);

        context.Entry(store).Property(item => item.Status)
            .CurrentValue = StoreStatus.Active;

        await context.SaveChangesAsync();

        StoreImageContentResult? visible = await service.OpenAsync(
            store.Id,
            kind);

        using Stream? visibleContent = visible?.Content;

        Assert.NotNull(visible);
        Assert.Equal(1, storage.OpenCount);
        Assert.Equal(
            kind == StoreImageKind.Logo
                ? store.LogoImageKey
                : store.CoverImageKey,
            storage.LastOpenedKey);
    }

    [Theory]
    [InlineData(StoreStatus.Active, false)]
    [InlineData(StoreStatus.PendingReview, false)]
    [InlineData(StoreStatus.Deleted, false)]
    [InlineData(StoreStatus.Draft, true)]
    public async Task NonEditableStoreDoesNotWriteFiles(
        StoreStatus status,
        bool softDeleted)
    {
        await using RentoXDbContext context = database.CreateContext();
        StoreProfile store = await CreateStoreAsync(
            context,
            status,
            softDeleted);

        StorageSpy storage = new();
        StoreImageService service = new(context, storage);

        using MemoryStream content = new();

        UploadStoreImageCommand command = new(
            store.OwnerId,
            StoreImageKind.Logo,
            content,
            "logo.png",
            "image/png",
            8);

        await Assert.ThrowsAsync<DomainException>(
            () => service.UploadAsync(command));

        await Assert.ThrowsAsync<DomainException>(
            () => service.DeleteAsync(
                store.OwnerId,
                StoreImageKind.Logo));

        Assert.Equal(0, storage.SaveCount);
        Assert.Equal(0, storage.DeleteCount);
    }

    [Theory]
    [InlineData(StoreImageKind.Logo, true)]
    [InlineData(StoreImageKind.Cover, true)]
    [InlineData(StoreImageKind.Logo, false)]
    [InlineData(StoreImageKind.Cover, false)]
    public async Task UploadValidatesImageBeforeStorage(
        StoreImageKind kind,
        bool valid)
    {
        await using RentoXDbContext context = database.CreateContext();
        StoreProfile store = await CreateStoreAsync(
            context,
            StoreStatus.Draft,
            false);

        string? previousKey = kind == StoreImageKind.Logo
            ? store.LogoImageKey
            : store.CoverImageKey;

        StorageSpy storage = new();
        StoreImageService service = new(context, storage);

        using MemoryStream content = new();

        if (valid)
        {
            using Image<Rgba32> image = new(2, 2);
            await image.SaveAsPngAsync(content);
        }
        else
        {
            byte[] bytes = Encoding.UTF8.GetBytes("This is not an image.");
            await content.WriteAsync(bytes.AsMemory());
        }

        content.Position = 0;

        UploadStoreImageCommand command = new(
            store.OwnerId,
            kind,
            content,
            "image.png",
            "image/png",
            content.Length);

        if (valid)
        {
            StoreImageResult result = await service.UploadAsync(command);

            Assert.Equal(store.Id, result.StoreId);
            Assert.Equal(kind, result.Kind);
            Assert.Equal(1, storage.SaveCount);
            Assert.Equal(content.Length, storage.SavedLength);
            Assert.Equal(
                kind == StoreImageKind.Logo
                    ? FileStorageArea.StoreLogos
                    : FileStorageArea.StoreCovers,
                storage.SavedArea);
            Assert.Equal(1, storage.DeleteCount);
            Assert.Equal(previousKey, storage.LastDeletedKey);
        }
        else
        {
            await Assert.ThrowsAsync<DomainException>(
                () => service.UploadAsync(command));

            Assert.Equal(0, storage.SaveCount);
            Assert.Equal(0, storage.DeleteCount);
        }

        context.ChangeTracker.Clear();

        StoreProfile persisted = await context.StoreProfiles
            .SingleAsync(item => item.Id == store.Id);

        string? currentKey = kind == StoreImageKind.Logo
            ? persisted.LogoImageKey
            : persisted.CoverImageKey;

        Assert.Equal(
            valid ? storage.SavedKey : previousKey,
            currentKey);
    }

    [Fact]
    public async Task UnknownStoreDoesNotOpenStorage()
    {
        await using RentoXDbContext context = database.CreateContext();
        StorageSpy storage = new();
        StoreImageService service = new(context, storage);

        StoreImageContentResult? result = await service.OpenAsync(
            Guid.NewGuid(),
            StoreImageKind.Logo);

        using Stream? content = result?.Content;

        Assert.Null(result);
        Assert.Equal(0, storage.OpenCount);
    }

    private static async Task<StoreProfile> CreateStoreAsync(
        RentoXDbContext context,
        StoreStatus status,
        bool softDeleted)
    {
        Guid ownerId = Guid.NewGuid();
        string userName = ownerId.ToString("N");
        DateTimeOffset now = DateTimeOffset.UtcNow;

        context.Users.Add(new AppUser
        {
            Id = ownerId,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            RegisteredAtUtc = now
        });

        StoreProfile store = StoreProfile.Create(
            ownerId,
            "Image test store",
            "image-test-" + Guid.NewGuid().ToString("N"),
            "Store created for isolated image tests.",
            "+994500000000",
            null,
            null,
            null,
            null,
            null,
            null);

        store.MarkAsCreated(now, ownerId);
        store.SetLogo("store-logos/" + Guid.NewGuid().ToString("N") + ".png");
        store.SetCover("store-covers/" + Guid.NewGuid().ToString("N") + ".png");

        context.StoreProfiles.Add(store);

        // Seed each persisted status independently of moderation workflows.
        context.Entry(store).Property(item => item.Status)
            .CurrentValue = status;

        if (softDeleted)
        {
            store.MarkAsDeleted(now);
        }

        await context.SaveChangesAsync();

        return store;
    }

    private sealed class StorageSpy : IFileStorage
    {
        public int OpenCount { get; private set; }
        public int SaveCount { get; private set; }
        public int DeleteCount { get; private set; }
        public string? LastOpenedKey { get; private set; }
        public string? LastDeletedKey { get; private set; }
        public string? SavedKey { get; private set; }
        public long SavedLength { get; private set; }
        public FileStorageArea SavedArea { get; private set; }

        public async Task<StoredFileResult> SaveAsync(
            FileStorageArea area,
            Stream content,
            string contentType,
            string extension,
            CancellationToken cancellationToken = default)
        {
            SaveCount++;
            SavedArea = area;

            using MemoryStream copy = new();
            await content.CopyToAsync(copy, cancellationToken);
            SavedLength = copy.Length;

            string folder = area == FileStorageArea.StoreLogos
                ? "store-logos"
                : "store-covers";

            string key =
                folder + "/" + Guid.NewGuid().ToString("N") + extension;

            SavedKey = key;

            return new StoredFileResult(
                key,
                contentType,
                SavedLength);
        }

        public Task<Stream?> OpenReadAsync(
            string storageKey,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OpenCount++;
            LastOpenedKey = storageKey;

            return Task.FromResult<Stream?>(new MemoryStream());
        }

        public Task DeleteAsync(
            string storageKey,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DeleteCount++;
            LastDeletedKey = storageKey;

            return Task.CompletedTask;
        }
    }
}