using Microsoft.EntityFrameworkCore;
using RentoX.Application.Files;
using RentoX.Application.Listings;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Listings;

public sealed class ListingImageManagementService(
    RentoXDbContext dbContext,
    IFileStorage fileStorage)
    : IListingImageManagementService
{
    public async Task<ListingImageContentResult?> OpenAsync(
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        ListingImage? image =
            await dbContext.ListingImages
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.Id == imageId,
                    cancellationToken);

        if (image is null)
        {
            return null;
        }

        Stream? content =
            await fileStorage.OpenReadAsync(
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