using RentoX.Domain.Common.Exceptions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.Processing;

namespace RentoX.Infrastructure.Files;

/// <summary>
/// Turns an already validated upload into what is kept on disk: upright,
/// at most <c>maxSide</c> pixels on the long side, WebP, without metadata
/// (camera model and GPS position never leave the phone). A 5 MB photo
/// typically becomes 150-250 KB.
/// </summary>
public static class ImageOptimizer
{
    public const int ListingMaxSide = 1600;
    public const int WebPQuality = 80;
    public const string WebPContentType = "image/webp";
    public const string WebPExtension = ".webp";

    // The caller owns both streams.
    public static async Task<MemoryStream> ToWebPAsync(
        Stream validated,
        int maxSide,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(validated);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxSide, 16);

        DecoderOptions options = new()
        {
            Configuration = ImageUploadValidator.DecoderConfiguration,
            // Orientation is read from the metadata, then dropped below.
            SkipMetadata = false,
            MaxFrames = 1
        };

        try
        {
            using Image image = await Image.LoadAsync(
                options,
                validated,
                cancellationToken);

            image.Mutate(context =>
            {
                context.AutoOrient();

                if (image.Width > maxSide || image.Height > maxSide)
                {
                    context.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(maxSide, maxSide),
                        Sampler = KnownResamplers.Lanczos3
                    });
                }
            });

            image.Metadata.ExifProfile = null;
            image.Metadata.IccProfile = null;
            image.Metadata.XmpProfile = null;
            image.Metadata.IptcProfile = null;

            MemoryStream output = new();

            try
            {
                await image.SaveAsync(
                    output,
                    new WebpEncoder
                    {
                        Quality = WebPQuality,
                        FileFormat = WebpFileFormatType.Lossy
                    },
                    cancellationToken);

                output.Position = 0;
                return output;
            }
            catch
            {
                await output.DisposeAsync();
                throw;
            }
        }
        catch (Exception exception) when (
            exception is UnknownImageFormatException
                or InvalidImageContentException
                or InvalidMemoryOperationException
                or NotSupportedException)
        {
            throw new DomainException(
                "Image content is invalid or cannot be decoded.");
        }
    }
}
