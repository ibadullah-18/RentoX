namespace RentoX.Application.Messaging;

// The caller owns upload streams; the HTTP response owns a returned download stream.
public sealed record MessageImageUpload(Stream Content, string ContentType, long SizeBytes);
public sealed record MessageImageDownload(Stream Content, string ContentType);

public interface IMessageImageService
{
    Task<MessageResult?> SendImagesAsync(
        Guid userId, Guid conversationId, string? body,
        IReadOnlyList<MessageImageUpload> images,
        CancellationToken cancellationToken = default);

    Task<MessageImageDownload?> OpenImageAsync(
        Guid userId, Guid conversationId, Guid imageId,
        CancellationToken cancellationToken = default);
}
