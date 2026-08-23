using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Listings;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Listings;

public sealed class ListingLifecycleService(
    RentoXDbContext dbContext,
    IClock clock)
    : IListingLifecycleService
{
    public async Task<ListingLifecycleResult>
        DeactivateAsync(
            Guid ownerId,
            Guid listingId,
            CancellationToken cancellationToken = default)
    {
        Listing listing =
            await GetOwnedListingAsync(
                ownerId,
                listingId,
                includeDeleted: false,
                cancellationToken);

        listing.Deactivate();

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return MapResult(listing);
    }

    public async Task<ListingLifecycleResult>
        ReactivateAsync(
            Guid ownerId,
            Guid listingId,
            CancellationToken cancellationToken = default)
    {
        Listing listing =
            await GetOwnedListingAsync(
                ownerId,
                listingId,
                includeDeleted: false,
                cancellationToken);

        listing.Reactivate(
            clock.UtcNow);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return MapResult(listing);
    }

    public async Task<ListingLifecycleResult>
        DeleteAsync(
            Guid ownerId,
            Guid listingId,
            CancellationToken cancellationToken = default)
    {
        Listing listing =
            await GetOwnedListingAsync(
                ownerId,
                listingId,
                includeDeleted: true,
                cancellationToken);

        listing.MarkDeleted(
            clock.UtcNow);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return MapResult(listing);
    }

    private async Task<Listing> GetOwnedListingAsync(
        Guid ownerId,
        Guid listingId,
        bool includeDeleted,
        CancellationToken cancellationToken)
    {
        Listing? listing =
            await dbContext.Listings
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == listingId &&
                        item.OwnerId == ownerId,
                    cancellationToken);

        if (listing is null ||
            (!includeDeleted &&
             listing.Status == ListingStatus.Deleted))
        {
            throw new DomainException(
                "Listing was not found.");
        }

        return listing;
    }

    private static ListingLifecycleResult MapResult(
        Listing listing)
    {
        return new ListingLifecycleResult(
            listing.Id,
            (int)listing.Status,
            listing.PublishedAtUtc,
            listing.ExpiresAtUtc,
            listing.DeletedAtUtc,
            listing.UpdatedAtUtc);
    }
}