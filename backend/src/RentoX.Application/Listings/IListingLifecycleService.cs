namespace RentoX.Application.Listings;

public interface IListingLifecycleService
{
    Task<ListingLifecycleResult> DeactivateAsync(
        Guid ownerId,
        Guid listingId,
        CancellationToken cancellationToken = default);

    Task<ListingLifecycleResult> ReactivateAsync(
        Guid ownerId,
        Guid listingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Live or paused listing -> draft, so it can be edited and then sent
    /// for review again.
    /// </summary>
    Task<ListingLifecycleResult> ReopenForEditingAsync(
        Guid ownerId,
        Guid listingId,
        CancellationToken cancellationToken = default);

    Task<ListingLifecycleResult> DeleteAsync(
        Guid ownerId,
        Guid listingId,
        CancellationToken cancellationToken = default);
}