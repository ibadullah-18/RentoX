namespace RentoX.Application.Files;

public interface IFileStorage
{
    Task<StoredFileResult> SaveAsync(
        FileStorageArea area,
        Stream content,
        string contentType,
        string extension,
        CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default);
}