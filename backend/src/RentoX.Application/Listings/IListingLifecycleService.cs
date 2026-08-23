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

    Task<ListingLifecycleResult> DeleteAsync(
        Guid ownerId,
        Guid listingId,
        CancellationToken cancellationToken = default);
}