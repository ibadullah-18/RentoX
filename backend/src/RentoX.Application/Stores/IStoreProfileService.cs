namespace RentoX.Application.Stores;

public interface IStoreProfileService
{
    /// <summary>The contact side (phone, social media, WhatsApp, hours): the owner may change it at any time.</summary>
    Task<StoreProfileResult> UpdateContactAsync(
        UpdateStoreContactCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>The name and description of a live store: the owner may change them at any time.</summary>
    Task<StoreProfileResult> UpdateInfoAsync(
        Guid ownerId,
        string name,
        string description,
        CancellationToken cancellationToken = default);

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