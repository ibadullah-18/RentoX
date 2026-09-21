using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Files;
using RentoX.Application.Messaging;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Messaging;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Messaging;

public sealed class MessageImageService(
    RentoXDbContext dbContext,
    IFileStorage fileStorage,
    IClock clock,
    IConversationEventPublisher eventPublisher,
    ILogger<MessageImageService> logger) : IMessageImageService
{
    private static readonly Action<ILogger, string, Exception?> CleanupFailed =
        LoggerMessage.Define<string>(LogLevel.Warning,
            new EventId(2201, "ChatImageCleanupFailed"),
            "Could not remove an uncommitted chat image {StorageKey}");

    public async Task<MessageResult?> SendImagesAsync(
        Guid userId, Guid conversationId, string? body,
        IReadOnlyList<MessageImageUpload> images,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(images);
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        var participants = await dbContext.Conversations.AsNoTracking()
            .Where(item => item.Id == conversationId &&
                (item.BuyerId == userId || item.SellerId == userId))
            .Select(item => new { item.BuyerId, item.SellerId })
            .SingleOrDefaultAsync(cancellationToken);
        if (participants is null) { return null; }

        if (images.Count is < 1 or > Message.MaximumImageCount)
        {
            throw new DomainException("A message must contain 1 to 5 images.");
        }
        if ((body?.Trim().Length ?? 0) > 2000)
        {
            throw new DomainException("Message cannot exceed 2000 characters.");
        }
        foreach (MessageImageUpload image in images)
        {
            ArgumentNullException.ThrowIfNull(image);
            ArgumentNullException.ThrowIfNull(image.Content);
            if (!image.Content.CanRead || image.SizeBytes is <= 0 or > MessageImage.MaximumSizeBytes)
            {
                throw new DomainException("Each image must be between 1 byte and 10 MB.");
            }
        }

        List<MessageImageFile> stored = [];
        bool commitAttempted = false;
        Message message;
        try
        {
            foreach (MessageImageUpload image in images)
            {
                await using MemoryStream content = await ReadBoundedAsync(image, cancellationToken);
                (string contentType, string extension) = DetectFormat(content.GetBuffer(), (int)content.Length);
                if (!string.Equals(contentType, image.ContentType, StringComparison.OrdinalIgnoreCase))
                {
                    throw new DomainException("Image content does not match its content type.");
                }
                content.Position = 0;
                StoredFileResult result = await fileStorage.SaveAsync(
                    FileStorageArea.ChatAttachments, content, contentType, extension, cancellationToken);
                stored.Add(new MessageImageFile(result.StorageKey, contentType, result.SizeBytes));
            }

            message = Message.CreateWithImages(conversationId, userId, body, clock.UtcNow, stored);
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            dbContext.Messages.Add(message);
            await dbContext.SaveChangesAsync(cancellationToken);
            // Once COMMIT has been sent, a lost connection can hide a successful commit.
            // Keep files in that case instead of deleting images of a saved message.
            commitAttempted = true;
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            if (!commitAttempted)
            {
                foreach (MessageImageFile file in stored)
                {
                    try { await fileStorage.DeleteAsync(file.StorageKey, CancellationToken.None); }
                    catch (Exception exception) { CleanupFailed(logger, file.StorageKey, exception); }
                }
            }
            throw;
        }

        MessageResult response = MessageResultMapper.Map(message);
        await eventPublisher.MessageCreatedAsync(
            participants.BuyerId, participants.SellerId, response, CancellationToken.None);
        return response;
    }

    public async Task<MessageImageDownload?> OpenImageAsync(
        Guid userId, Guid conversationId, Guid imageId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty) { return null; }
        var image = await (
            from attachment in dbContext.Set<MessageImage>().AsNoTracking()
            join message in dbContext.Messages.AsNoTracking() on attachment.MessageId equals message.Id
            join conversation in dbContext.Conversations.AsNoTracking() on message.ConversationId equals conversation.Id
            where attachment.Id == imageId && conversation.Id == conversationId &&
                (conversation.BuyerId == userId || conversation.SellerId == userId)
            select new { attachment.StorageKey, attachment.ContentType })
            .SingleOrDefaultAsync(cancellationToken);
        if (image is null) { return null; }

        Stream? content = await fileStorage.OpenReadAsync(image.StorageKey, cancellationToken);
        return content is null ? null : new MessageImageDownload(content, image.ContentType);
    }

    private static async Task<MemoryStream> ReadBoundedAsync(
        MessageImageUpload image, CancellationToken cancellationToken)
    {
        MemoryStream output = new((int)image.SizeBytes);
        try
        {
            byte[] buffer = new byte[81_920];
            int read;
            while ((read = await image.Content.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
            {
                if (output.Length + read > MessageImage.MaximumSizeBytes)
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
        if (length >= 16 && data.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
            data.AsSpan(8, 4).SequenceEqual("WEBP"u8) &&
            (data.AsSpan(12, 4).SequenceEqual("VP8 "u8) ||
             data.AsSpan(12, 4).SequenceEqual("VP8L"u8) ||
             data.AsSpan(12, 4).SequenceEqual("VP8X"u8)))
        {
            return ("image/webp", ".webp");
        }
        throw new DomainException("Only JPEG, PNG and WebP image content is supported.");
    }
}
