using Microsoft.EntityFrameworkCore;
using RentoX.Application.Files;
using RentoX.Application.Listings;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Listings;

public sealed class ListingImageManagementService(
    RentoXDbContext dbContext,
    IFileStorage fileStorage,
    RentoX.Application.Abstractions.Time.IClock clock)
    : IListingImageManagementService
{
    public Task<ListingImageContentResult?> OpenAsync(
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        return OpenAsync(
            imageId,
            null,
            false,
            cancellationToken);
    }

    public async Task<ListingImageContentResult?> OpenAsync(
        Guid imageId,
        Guid? viewerUserId,
        bool canModerate,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (imageId == Guid.Empty)
        {
            return null;
        }

        Guid? validViewerId =
            viewerUserId.HasValue && viewerUserId.Value != Guid.Empty
                ? viewerUserId
                : null;

        bool hasModerationAccess =
            validViewerId.HasValue && canModerate;

        DateTimeOffset utcNow = clock.UtcNow;

        var image = await (
            from item in dbContext.ListingImages.AsNoTracking()
            join listing in dbContext.Listings.AsNoTracking()
                on item.ListingId equals listing.Id
            where item.Id == imageId
                && listing.DeletedAtUtc == null
                && listing.Status != ListingStatus.Deleted
                && (
                    hasModerationAccess
                    || (validViewerId != null
                        && listing.OwnerId == validViewerId)
                    || (listing.Status == ListingStatus.Active
                        && listing.PublishedAtUtc != null
                        && listing.PublishedAtUtc <= utcNow
                        && listing.ExpiresAtUtc != null
                        && listing.ExpiresAtUtc > utcNow)
                )
            select new
            {
                item.StorageKey,
                item.ContentType
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (image is null)
        {
            return null;
        }

        Stream? content = await fileStorage.OpenReadAsync(
            image.StorageKey,
            cancellationToken);

        return content is null
            ? null
            : new ListingImageContentResult(
                content,
                image.ContentType);
    }

    public async Task DeleteAsync(
        Guid ownerId,
        Guid listingId,
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        Listing listing =
            await GetOwnedListingAsync(
                ownerId,
                listingId,
                cancellationToken);

        ListingImage removedImage =
            listing.RemoveImage(imageId);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await fileStorage.DeleteAsync(
            removedImage.StorageKey,
            cancellationToken);
    }

    public async Task SetCoverAsync(
        Guid ownerId,
        Guid listingId,
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        Listing listing =
            await GetOwnedListingAsync(
                ownerId,
                listingId,
                cancellationToken);

        listing.SetCoverImage(imageId);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task ReorderAsync(
        Guid ownerId,
        Guid listingId,
        IReadOnlyList<Guid> orderedImageIds,
        CancellationToken cancellationToken = default)
    {
        Listing listing =
            await GetOwnedListingAsync(
                ownerId,
                listingId,
                cancellationToken);

        listing.ReorderImages(orderedImageIds);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private async Task<Listing> GetOwnedListingAsync(
        Guid ownerId,
        Guid listingId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Listings
            .Include(listing => listing.Images)
            .SingleOrDefaultAsync(
                listing =>
                    listing.Id == listingId &&
                    listing.OwnerId == ownerId,
                cancellationToken)
            ?? throw new DomainException(
                "Listing was not found.");
    }
}
