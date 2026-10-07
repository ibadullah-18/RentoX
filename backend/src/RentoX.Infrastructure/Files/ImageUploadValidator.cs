using RentoX.Domain.Common.Exceptions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Memory;

namespace RentoX.Infrastructure.Files;

public static class ImageUploadValidator
{
    public const int MaximumFileSizeBytes = 10 * 1024 * 1024;
    public const int MaximumDimension = 12_000;
    public const long MaximumPixelCount = 50_000_000;

    private static readonly Configuration DecoderConfiguration =
        CreateConfiguration();

    // The caller owns both the input stream and the returned buffer.
    public static async Task<MemoryStream> ReadValidatedAsync(
        Stream source,
        long declaredSize,
        string contentType,
        string extension,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        cancellationToken.ThrowIfCancellationRequested();

        if (!source.CanRead)
        {
            throw new DomainException("Image stream is not readable.");
        }

        if (declaredSize is <= 0 or > MaximumFileSizeBytes)
        {
            throw new DomainException(
                "Image must be between 1 byte and 10 MB.");
        }

        string normalizedExtension =
            (extension ?? string.Empty).Trim().ToLowerInvariant();

        string expectedContentType = normalizedExtension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => throw new DomainException(
                "Only JPEG, PNG and WebP images are supported.")
        };

        if (!string.Equals(
                expectedContentType,
                contentType?.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException(
                "Image extension and content type do not match.");
        }

        MemoryStream buffer = new((int)declaredSize);

        try
        {
            byte[] chunk = new byte[81_920];
            int read;

            while ((read = await source.ReadAsync(
                       chunk.AsMemory(),
                       cancellationToken)) > 0)
            {
                if (buffer.Length + read > MaximumFileSizeBytes ||
                    buffer.Length + read > declaredSize)
                {
                    throw new DomainException(
                        "Image exceeds its declared size or the 10 MB limit.");
                }

                await buffer.WriteAsync(
                    chunk.AsMemory(0, read),
                    cancellationToken);
            }

            if (buffer.Length != declaredSize)
            {
                throw new DomainException(
                    "Image length does not match its declared size.");
            }

            buffer.Position = 0;

            try
            {
                IImageFormat format = await Image.DetectFormatAsync(
                    buffer,
                    cancellationToken);

                if (!string.Equals(
                        format.DefaultMimeType,
                        expectedContentType,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new DomainException(
                        "Image content does not match its declared format.");
                }

                DecoderOptions options = new()
                {
                    Configuration = DecoderConfiguration,
                    SkipMetadata = true,
                    MaxFrames = 2
                };

                buffer.Position = 0;

                ImageInfo info = await Image.IdentifyAsync(
                    options,
                    buffer,
                    cancellationToken);

                if (info.Width <= 0 ||
                    info.Height <= 0 ||
                    info.Width > MaximumDimension ||
                    info.Height > MaximumDimension ||
                    (long)info.Width * info.Height > MaximumPixelCount)
                {
                    throw new DomainException(
                        "Image dimensions exceed the supported limit.");
                }

                if (info.FrameMetadataCollection.Count > 1)
                {
                    throw new DomainException(
                        "Animated images are not supported.");
                }

                buffer.Position = 0;

                using Image decoded = await Image.LoadAsync(
                    options,
                    buffer,
                    cancellationToken);

                if (decoded.Frames.Count != 1)
                {
                    throw new DomainException(
                        "Animated images are not supported.");
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

            buffer.Position = 0;
            return buffer;
        }
        catch
        {
            buffer.Dispose();
            throw;
        }
    }

    private static Configuration CreateConfiguration()
    {
        Configuration configuration = Configuration.Default.Clone();

        configuration.MaxDegreeOfParallelism = 2;
        configuration.MemoryAllocator = MemoryAllocator.Create(
            new MemoryAllocatorOptions
            {
                MaximumPoolSizeMegabytes = 16,
                AllocationLimitMegabytes = 256
            });

        return configuration;
    }
}
