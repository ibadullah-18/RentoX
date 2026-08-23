using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Files;
using RentoX.Application.Listings;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Listings;

public sealed partial class ListingMaintenanceService(
    RentoXDbContext dbContext,
    IFileStorage fileStorage,
    IClock clock,
    ILogger<ListingMaintenanceService> logger)
    : IListingMaintenanceService
{
    public async Task<ListingMaintenanceResult> RunAsync(
    TimeSpan deletedImageRetention,
    int cleanupBatchSize,
    CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(
            deletedImageRetention,
            TimeSpan.Zero);

        ArgumentOutOfRangeException.ThrowIfLessThan(
            cleanupBatchSize,
            1);

        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            cleanupBatchSize,
            1000);

        DateTimeOffset now = clock.UtcNow;

        int expiredListingCount =
            await ExpireListingsAsync(
                now,
                cancellationToken);

        ImageCleanupResult cleanupResult =
            await PurgeDeletedImagesAsync(
                now.Subtract(deletedImageRetention),
                cleanupBatchSize,
                cancellationToken);

        return new ListingMaintenanceResult(
            expiredListingCount,
            cleanupResult.PurgedCount,
            cleanupResult.FailedCount);
    }

    private Task<int> ExpireListingsAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        return dbContext.Listings
            .Where(listing =>
                (listing.Status == ListingStatus.Active ||
                 listing.Status ==
                    ListingStatus.Deactivated) &&
                listing.ExpiresAtUtc.HasValue &&
                listing.ExpiresAtUtc.Value <= now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        listing => listing.Status,
                        ListingStatus.Expired)
                    .SetProperty(
                        listing => listing.UpdatedAtUtc,
                        now),
                cancellationToken);
    }

    private async Task<ImageCleanupResult>
        PurgeDeletedImagesAsync(
            DateTimeOffset deletionCutoff,
            int cleanupBatchSize,
            CancellationToken cancellationToken)
    {
        List<ListingImage> images =
            await dbContext.ListingImages
                .Where(image =>
                    dbContext.Listings.Any(listing =>
                        listing.Id == image.ListingId &&
                        listing.Status ==
                            ListingStatus.Deleted &&
                        listing.DeletedAtUtc.HasValue &&
                        listing.DeletedAtUtc.Value <=
                            deletionCutoff))
                .OrderBy(image => image.Id)
                .Take(cleanupBatchSize)
                .ToListAsync(cancellationToken);

        int purgedCount = 0;
        int failedCount = 0;

        foreach (ListingImage image in images)
        {
            try
            {
                await fileStorage.DeleteAsync(
                    image.StorageKey,
                    cancellationToken);

                dbContext.ListingImages.Remove(image);
                purgedCount++;
            }
            catch (IOException exception)
            {
                failedCount++;

                LogImageDeletionFailure(
                    logger,
                    image.StorageKey,
                    exception);
            }
            catch (UnauthorizedAccessException exception)
            {
                failedCount++;

                LogImageDeletionFailure(
                    logger,
                    image.StorageKey,
                    exception);
            }
            catch (DomainException exception)
            {
                failedCount++;

                LogImageDeletionFailure(
                    logger,
                    image.StorageKey,
                    exception);
            }
        }

        if (purgedCount > 0)
        {
            await dbContext.SaveChangesAsync(
                cancellationToken);
        }

        return new ImageCleanupResult(
            purgedCount,
            failedCount);
    }

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Error,
        Message =
            "Failed to delete listing image {StorageKey}")]
    private static partial void LogImageDeletionFailure(
        ILogger logger,
        string storageKey,
        Exception exception);

    private sealed record ImageCleanupResult(
        int PurgedCount,
        int FailedCount);
}