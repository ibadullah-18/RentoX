using System.Text;
using Microsoft.EntityFrameworkCore;
using RentoX.Application.Files;
using RentoX.Application.Listings;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Infrastructure.Files;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Listings;
using RentoX.Infrastructure.Persistence;
using RentoX.IntegrationTests.Auditing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

namespace RentoX.IntegrationTests.Listings;

public sealed class ListingImageUploadSafetyTests(
    AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    [Theory]
    [InlineData("png", "image/png", ".png")]
    [InlineData("jpeg", "image/jpeg", ".jpg")]
    [InlineData("webp", "image/webp", ".webp")]
    public async Task ValidFormatsAreAccepted(
        string format,
        string contentType,
        string extension)
    {
        byte[] bytes = await CreateImageAsync(format);
        using MemoryStream input = new(bytes, writable: false);

        using MemoryStream result =
            await ImageUploadValidator.ReadValidatedAsync(
                input, bytes.LongLength, contentType, extension);

        Assert.Equal(0L, result.Position);
        Assert.Equal(bytes, result.ToArray());
        Assert.True(input.CanRead);
    }

    [Theory]
    [InlineData("text")]
    [InlineData("pngHeader")]
    [InlineData("jpegHeader")]
    public async Task FakeImagesAreRejected(string kind)
    {
        byte[] bytes = kind switch
        {
            "pngHeader" => [137, 80, 78, 71, 13, 10, 26, 10],
            "jpegHeader" => [255, 216, 255],
            _ => Encoding.UTF8.GetBytes("This is not an image.")
        };

        bool jpeg = kind == "jpegHeader";
        using MemoryStream input = new(bytes, writable: false);

        await Assert.ThrowsAsync<DomainException>(() =>
            ImageUploadValidator.ReadValidatedAsync(
                input,
                bytes.LongLength,
                jpeg ? "image/jpeg" : "image/png",
                jpeg ? ".jpg" : ".png"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public async Task IncorrectDeclaredSizeIsRejected(int difference)
    {
        byte[] bytes = await CreateImageAsync("png");
        using MemoryStream input = new(bytes, writable: false);

        await Assert.ThrowsAsync<DomainException>(() =>
            ImageUploadValidator.ReadValidatedAsync(
                input,
                bytes.LongLength + difference,
                "image/png",
                ".png"));
    }

    [Fact]
    public async Task ActualFormatMustMatchContentType()
    {
        byte[] bytes = await CreateImageAsync("png");
        using MemoryStream input = new(bytes, writable: false);

        await Assert.ThrowsAsync<DomainException>(() =>
            ImageUploadValidator.ReadValidatedAsync(
                input, bytes.LongLength, "image/jpeg", ".jpg"));
    }

    [Fact]
    public async Task ExtensionMustMatchContentType()
    {
        byte[] bytes = await CreateImageAsync("png");
        using MemoryStream input = new(bytes, writable: false);

        await Assert.ThrowsAsync<DomainException>(() =>
            ImageUploadValidator.ReadValidatedAsync(
                input, bytes.LongLength, "image/png", ".jpg"));
    }

    [Fact]
    public async Task OversizedDeclarationIsRejectedBeforeReading()
    {
        using MemoryStream input = new();

        await Assert.ThrowsAsync<DomainException>(() =>
            ImageUploadValidator.ReadValidatedAsync(
                input,
                ImageUploadValidator.MaximumFileSizeBytes + 1L,
                "image/png",
                ".png"));

        Assert.Equal(0L, input.Position);
    }

    [Fact]
    public async Task ActualBytesCannotExceedTheLimit()
    {
        byte[] bytes =
            new byte[ImageUploadValidator.MaximumFileSizeBytes + 1];

        using MemoryStream input = new(bytes, writable: false);

        await Assert.ThrowsAsync<DomainException>(() =>
            ImageUploadValidator.ReadValidatedAsync(
                input,
                ImageUploadValidator.MaximumFileSizeBytes,
                "image/png",
                ".png"));
    }

    [Fact]
    public async Task ExcessiveDimensionsAreRejected()
    {
        byte[] bytes = await CreateImageAsync(
            "png",
            ImageUploadValidator.MaximumDimension + 1);

        using MemoryStream input = new(bytes, writable: false);

        await Assert.ThrowsAsync<DomainException>(() =>
            ImageUploadValidator.ReadValidatedAsync(
                input, bytes.LongLength, "image/png", ".png"));
    }

    [Fact]
    public async Task AnimatedWebpIsRejected()
    {
        byte[] bytes = await CreateImageAsync(
            "webp",
            animated: true);

        using MemoryStream input = new(bytes, writable: false);

        await Assert.ThrowsAsync<DomainException>(() =>
            ImageUploadValidator.ReadValidatedAsync(
                input, bytes.LongLength, "image/webp", ".webp"));
    }

    [Fact]
    public async Task CancelledValidationDoesNotReadTheInput()
    {
        using MemoryStream input = new();
        using CancellationTokenSource cancellation = new();

        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ImageUploadValidator.ReadValidatedAsync(
                input, 1, "image/png", ".png", cancellation.Token));

        Assert.Equal(0L, input.Position);
    }

    [Fact]
    public async Task ServiceStoresAndPersistsAValidatedImage()
    {
        SeedData seed = await SeedAsync();
        byte[] bytes = await CreateImageAsync("png");

        using MemoryStream input = new(bytes, writable: false);
        await using RentoXDbContext db = database.CreateContext();
        StorageSpy storage = new();
        ListingImageService service = new(db, storage);

        ListingImageResult result = await service.UploadAsync(
            new UploadListingImageCommand(
                seed.OwnerId,
                seed.ListingId,
                input,
                "photo.png",
                "image/png",
                bytes.LongLength));

        Assert.Equal(1, storage.SaveCount);
        Assert.Equal(bytes.LongLength, storage.SavedSize);
        Assert.Equal("image/png", storage.SavedContentType);

        await using RentoXDbContext verification =
            database.CreateContext();

        ListingImage saved = await verification.ListingImages
            .SingleAsync(item => item.Id == result.Id);

        Assert.Equal(seed.ListingId, saved.ListingId);
        Assert.Equal(bytes.LongLength, saved.SizeBytes);
        Assert.Equal(storage.SavedKey, saved.StorageKey);
    }

    [Fact]
    public async Task ServiceRejectsFakeImageBeforeStorageAndPersistence()
    {
        SeedData seed = await SeedAsync();
        byte[] bytes = Encoding.UTF8.GetBytes("fake image");

        using MemoryStream input = new(bytes, writable: false);
        await using RentoXDbContext db = database.CreateContext();
        StorageSpy storage = new();
        ListingImageService service = new(db, storage);

        await Assert.ThrowsAsync<DomainException>(() =>
            service.UploadAsync(
                new UploadListingImageCommand(
                    seed.OwnerId,
                    seed.ListingId,
                    input,
                    "fake.png",
                    "image/png",
                    bytes.LongLength)));

        Assert.Equal(0, storage.SaveCount);

        await using RentoXDbContext verification =
            database.CreateContext();

        Assert.False(await verification.ListingImages.AnyAsync(
            item => item.ListingId == seed.ListingId));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OtherOwnerOrDeletedListingCannotReceiveImages(
        bool deleted)
    {
        SeedData seed = await SeedAsync(deleted);
        byte[] bytes = await CreateImageAsync("png");

        using MemoryStream input = new(bytes, writable: false);
        await using RentoXDbContext db = database.CreateContext();
        StorageSpy storage = new();
        ListingImageService service = new(db, storage);

        await Assert.ThrowsAsync<DomainException>(() =>
            service.UploadAsync(
                new UploadListingImageCommand(
                    deleted ? seed.OwnerId : Guid.NewGuid(),
                    seed.ListingId,
                    input,
                    "photo.png",
                    "image/png",
                    bytes.LongLength)));

        Assert.Equal(0, storage.SaveCount);
        Assert.Equal(0L, input.Position);
    }

    private async Task<SeedData> SeedAsync(bool deleted = false)
    {
        await using RentoXDbContext db = database.CreateContext();

        DateTimeOffset now = DateTimeOffset.UtcNow;
        Guid ownerId = Guid.NewGuid();
        string name = $"upload-test-{ownerId:N}";

        AppUser owner = new()
        {
            Id = ownerId,
            UserName = name,
            NormalizedUserName = name.ToUpperInvariant(),
            RegisteredAtUtc = now,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };

        Category category = Category.Create(
            null, $"upload-{Guid.NewGuid():N}", null, 0);

        Listing listing = Listing.Create(
            ownerId,
            category.Id,
            "Image upload test listing",
            "A listing used only by isolated image upload tests.",
            100m,
            "AZN",
            (RentalPeriodUnit)2);

        listing.MarkAsCreated(now, ownerId);

        if (deleted)
        {
            listing.MarkAsDeleted(now);
        }

        db.Users.Add(owner);
        db.Categories.Add(category);
        db.Listings.Add(listing);

        await db.SaveChangesAsync();

        return new SeedData(ownerId, listing.Id);
    }

    private static async Task<byte[]> CreateImageAsync(
        string format,
        int width = 2,
        bool animated = false)
    {
        using Image<Rgba32> image = new(width, 2);
        image[0, 0] = new Rgba32(255, 0, 0);

        if (animated)
        {
            ImageFrame<Rgba32> second =
                image.Frames.AddFrame(image.Frames.RootFrame);

            second[0, 0] = new Rgba32(0, 255, 0);
        }

        IImageEncoder encoder = format switch
        {
            "png" => new PngEncoder(),
            "jpeg" => new JpegEncoder(),
            "webp" => new WebpEncoder(),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

        using MemoryStream output = new();
        await image.SaveAsync(output, encoder);

        return output.ToArray();
    }

    private sealed record SeedData(Guid OwnerId, Guid ListingId);

    private sealed class StorageSpy : IFileStorage
    {
        public int SaveCount { get; private set; }
        public long SavedSize { get; private set; }
        public string? SavedContentType { get; private set; }
        public string? SavedKey { get; private set; }

        public async Task<StoredFileResult> SaveAsync(
            FileStorageArea area,
            Stream content,
            string contentType,
            string extension,
            CancellationToken cancellationToken = default)
        {
            SaveCount++;

            using MemoryStream output = new();
            await content.CopyToAsync(output, cancellationToken);

            SavedSize = output.Length;
            SavedContentType = contentType;

            string key =
                $"listing-images/{Guid.NewGuid():N}{extension}";

            SavedKey = key;

            return new StoredFileResult(
                key,
                contentType,
                SavedSize);
        }

        public Task<Stream?> OpenReadAsync(
            string storageKey,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Upload tests must not read stored files.");
        }

        public Task DeleteAsync(
            string storageKey,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
