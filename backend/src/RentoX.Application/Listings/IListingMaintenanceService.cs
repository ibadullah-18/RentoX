namespace RentoX.Application.Listings;

public interface IListingMaintenanceService
{
    Task<ListingMaintenanceResult> RunAsync(
        TimeSpan deletedImageRetention,
        int cleanupBatchSize,
        CancellationToken cancellationToken = default);
}