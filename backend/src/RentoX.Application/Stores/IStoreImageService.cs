namespace RentoX.Application.Stores;

public interface IStoreImageService
{
    Task<StoreImageResult> UploadAsync(
        UploadStoreImageCommand command,
        CancellationToken cancellationToken = default);

    Task<StoreImageContentResult?> OpenAsync(
        Guid storeId,
        StoreImageKind kind,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid ownerId,
        StoreImageKind kind,
        CancellationToken cancellationToken = default);
}