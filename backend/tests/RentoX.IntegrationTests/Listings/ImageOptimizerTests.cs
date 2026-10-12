using RentoX.Domain.Common.Exceptions;
using RentoX.Infrastructure.Files;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;

namespace RentoX.IntegrationTests.Listings;

public sealed class ImageOptimizerTests
{
    private static async Task<MemoryStream> PhotoAsync(
        int width,
        int height,
        bool png = false,
        ushort? orientation = null)
    {
        using Image<Rgba32> image = new(width, height);

        // Noise-like content so the original is not trivially tiny.
        Random random = new(7);

        for (int y = 0; y < height; y += 4)
        {
            for (int x = 0; x < width; x += 4)
            {
                Rgba32 color = new(
                    (byte)random.Next(256),
                    (byte)random.Next(256),
                    (byte)random.Next(256));

                for (int dy = 0; dy < 4 && y + dy < height; dy++)
                {
                    for (int dx = 0; dx < 4 && x + dx < width; dx++)
                    {
                        image[x + dx, y + dy] = color;
                    }
                }
            }
        }

        if (orientation.HasValue)
        {
            image.Metadata.ExifProfile = new ExifProfile();
            image.Metadata.ExifProfile.SetValue(
                ExifTag.Orientation,
                orientation.Value);
            image.Metadata.ExifProfile.SetValue(
                ExifTag.Make,
                "SecretPhoneCo");
        }

        MemoryStream stream = new();

        if (png)
        {
            await image.SaveAsync(stream, new PngEncoder());
        }
        else
        {
            await image.SaveAsync(stream, new JpegEncoder { Quality = 95 });
        }

        stream.Position = 0;
        return stream;
    }

    [Theory]
    [InlineData(4000, 3000, 1600, 1200)]
    [InlineData(3000, 4000, 1200, 1600)]
    [InlineData(1600, 1000, 1600, 1000)]
    [InlineData(640, 480, 640, 480)]
    public async Task ImagesAreShrunkToTheLimitNeverEnlarged(
        int width,
        int height,
        int expectedWidth,
        int expectedHeight)
    {
        using MemoryStream original = await PhotoAsync(width, height);

        using MemoryStream result = await ImageOptimizer.ToWebPAsync(
            original,
            ImageOptimizer.ListingMaxSide);

        IImageFormat format = await Image.DetectFormatAsync(result);
        Assert.Equal("image/webp", format.DefaultMimeType);

        result.Position = 0;
        ImageInfo info = await Image.IdentifyAsync(result);
        Assert.Equal(expectedWidth, info.Width);
        Assert.Equal(expectedHeight, info.Height);
    }

    [Fact]
    public async Task ABigPhotoBecomesMuchSmaller()
    {
        using MemoryStream original = await PhotoAsync(4000, 3000);
        long before = original.Length;

        using MemoryStream result = await ImageOptimizer.ToWebPAsync(
            original,
            ImageOptimizer.ListingMaxSide);

        Assert.True(
            result.Length * 5 < before,
            $"{before} bytes became {result.Length} bytes");
    }

    [Fact]
    public async Task PngBecomesWebPToo()
    {
        using MemoryStream original =
            await PhotoAsync(2400, 1800, png: true);

        using MemoryStream result = await ImageOptimizer.ToWebPAsync(
            original,
            ImageOptimizer.ListingMaxSide);

        IImageFormat format = await Image.DetectFormatAsync(result);
        Assert.Equal("image/webp", format.DefaultMimeType);
        Assert.True(result.Length < original.Length);
    }

    [Fact]
    public async Task RotatedPhotosAreStoredUprightWithoutMetadata()
    {
        // Orientation 6 = the phone was held sideways: shown rotated 90°.
        using MemoryStream original = await PhotoAsync(
            800,
            400,
            orientation: 6);

        using MemoryStream result = await ImageOptimizer.ToWebPAsync(
            original,
            ImageOptimizer.ListingMaxSide);

        using Image stored = await Image.LoadAsync(result);

        Assert.Equal(400, stored.Width);
        Assert.Equal(800, stored.Height);
        Assert.Null(stored.Metadata.ExifProfile);
    }

    [Fact]
    public async Task GarbageIsRejectedAsInvalidContent()
    {
        using MemoryStream garbage = new([1, 2, 3, 4, 5, 6, 7, 8]);

        await Assert.ThrowsAsync<DomainException>(() =>
            ImageOptimizer.ToWebPAsync(
                garbage,
                ImageOptimizer.ListingMaxSide));
    }
}
