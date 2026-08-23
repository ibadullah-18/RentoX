namespace RentoX.Application.Stores;

public interface IStoreProfileService
{
    Task<StoreProfileResult> CreateAsync(
        CreateStoreCommand command,
        CancellationToken cancellationToken = default);

    Task<StoreProfileResult> UpdateAsync(
        UpdateStoreCommand command,
        CancellationToken cancellationToken = default);

    Task<StoreProfileResult?> GetMineAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default);
}