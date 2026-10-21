using Microsoft.Extensions.Logging;
using RentoX.Application.Files;
using RentoX.Application.Support;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Support;

namespace RentoX.Infrastructure.Support;

/// <summary>Checks and keeps the photos of a support message, and takes them back when the message is not saved.</summary>
internal static class SupportImageStore
{
    public static void Validate(IReadOnlyList<SupportImageUpload>? images, string? body)
    {
        if (images is null || images.Count == 0)
        {
            return;
        }

        if (images.Count > SupportTicketImage.MaximumPerMessage)
        {
            throw new DomainException("A message can have at most 5 images.");
        }

        if ((body?.Trim().Length ?? 0) > 4000)
        {
            throw new DomainException("Support message body cannot exceed 4000 characters.");
        }

        foreach (SupportImageUpload image in images)
        {
            ArgumentNullException.ThrowIfNull(image);
            ArgumentNullException.ThrowIfNull(image.Content);
            if (!image.Content.CanRead || image.SizeBytes is <= 0 or > SupportTicketImage.MaximumSizeBytes)
            {
                throw new DomainException("Each image must be between 1 byte and 10 MB.");
            }
        }
    }

    public static async Task<List<SupportTicketImageFile>> StoreAsync(
        IFileStorage storage, IReadOnlyList<SupportImageUpload> images, CancellationToken cancellationToken)
    {
        List<SupportTicketImageFile> stored = [];
        try
        {
            foreach (SupportImageUpload image in images)
            {
                await using MemoryStream content = await ReadBoundedAsync(image, cancellationToken);
                (string contentType, string extension) = DetectFormat(content.GetBuffer(), (int)content.Length);
                if (!string.Equals(contentType, image.ContentType, StringComparison.OrdinalIgnoreCase))
                {
                    throw new DomainException("Image content does not match its content type.");
                }

                content.Position = 0;
                StoredFileResult result = await storage.SaveAsync(
                    FileStorageArea.SupportAttachments, content, contentType, extension, cancellationToken);
                stored.Add(new SupportTicketImageFile(result.StorageKey, contentType, result.SizeBytes));
            }
        }
        catch
        {
            await DiscardAsync(storage, stored, null);
            throw;
        }

        return stored;
    }

    public static async Task DiscardAsync(
        IFileStorage storage, IEnumerable<SupportTicketImageFile> files, ILogger? logger)
    {
        foreach (SupportTicketImageFile file in files)
        {
            try
            {
                await storage.DeleteAsync(file.StorageKey, CancellationToken.None);
            }
            catch (Exception exception) when (logger is not null)
            {
#pragma warning disable CA1848
                logger.LogWarning(exception, "Could not remove an uncommitted support image {StorageKey}", file.StorageKey);
#pragma warning restore CA1848
            }
            catch
            {
                // Nothing more can be done for a file that is not saved anywhere.
            }
        }
    }

    private static async Task<MemoryStream> ReadBoundedAsync(SupportImageUpload image, CancellationToken cancellationToken)
    {
        MemoryStream output = new((int)image.SizeBytes);
        try
        {
            byte[] buffer = new byte[81_920];
            int read;
            while ((read = await image.Content.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
            {
                if (output.Length + read > SupportTicketImage.MaximumSizeBytes)
                {
                    throw new DomainException("Image cannot exceed 10 MB.");
                }

                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }

            if (output.Length != image.SizeBytes)
            {
                throw new DomainException("Image length does not match the uploaded file.");
            }

            return output;
        }
        catch
        {
            await output.DisposeAsync();
            throw;
        }
    }

    private static (string ContentType, string Extension) DetectFormat(byte[] data, int length)
    {
        if (length >= 3 && data[0] == 0xff && data[1] == 0xd8 && data[2] == 0xff)
        {
            return ("image/jpeg", ".jpg");
        }

        ReadOnlySpan<byte> png = [137, 80, 78, 71, 13, 10, 26, 10];
        if (length >= 8 && data.AsSpan(0, 8).SequenceEqual(png))
        {
            return ("image/png", ".png");
        }

        if (length >= 16 && data.AsSpan(0, 4).SequenceEqual("RIFF"u8) && data.AsSpan(8, 4).SequenceEqual("WEBP"u8) &&
            (data.AsSpan(12, 4).SequenceEqual("VP8 "u8) || data.AsSpan(12, 4).SequenceEqual("VP8L"u8) ||
             data.AsSpan(12, 4).SequenceEqual("VP8X"u8)))
        {
            return ("image/webp", ".webp");
        }

        throw new DomainException("Only JPEG, PNG and WebP image content is supported.");
    }
}
