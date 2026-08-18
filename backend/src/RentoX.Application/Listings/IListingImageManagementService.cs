namespace RentoX.Application.Listings;

public interface IListingImageManagementService
{
    Task<ListingImageContentResult?> OpenAsync(
        Guid imageId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid ownerId,
        Guid listingId,
        Guid imageId,
        CancellationToken cancellationToken = default);

    Task SetCoverAsync(
        Guid ownerId,
        Guid listingId,
        Guid imageId,
        CancellationToken cancellationToken = default);

    Task ReorderAsync(
        Guid ownerId,
        Guid listingId,
        IReadOnlyList<Guid> orderedImageIds,
        CancellationToken cancellationToken = default);
}