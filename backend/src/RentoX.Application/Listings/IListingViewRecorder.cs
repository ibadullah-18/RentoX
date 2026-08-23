namespace RentoX.Application.Listings;

public interface IListingViewRecorder
{
    Task<long> RecordAsync(
        Guid listingId,
        Guid? viewerUserId,
        CancellationToken cancellationToken = default);
}