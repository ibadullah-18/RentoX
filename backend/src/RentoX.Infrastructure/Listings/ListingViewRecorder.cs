using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Listings;
using RentoX.Domain.Listings;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Listings;

public sealed class ListingViewRecorder(
    RentoXDbContext dbContext,
    IClock clock)
    : IListingViewRecorder
{
    public async Task<long> RecordAsync(
        Guid listingId,
        Guid? viewerUserId,
        CancellationToken cancellationToken = default)
    {
        bool shouldIncrement;

        if (viewerUserId.HasValue)
        {
            Guid viewId = Guid.NewGuid();
            DateTimeOffset viewedAtUtc = clock.UtcNow;

            int insertedRows =
                await dbContext.Database
                    .ExecuteSqlInterpolatedAsync(
                        $"""
                        INSERT INTO listings.listing_views
                            ("Id", "ListingId", "ViewerUserId", "ViewedAtUtc")
                        VALUES
                            ({viewId}, {listingId}, {viewerUserId.Value}, {viewedAtUtc})
                        ON CONFLICT ("ListingId", "ViewerUserId")
                        DO NOTHING
                        """,
                        cancellationToken);

            shouldIncrement = insertedRows > 0;
        }
        else
        {
            shouldIncrement = true;
        }

        if (shouldIncrement)
        {
            await dbContext.Listings
                .Where(listing =>
                    listing.Id == listingId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        listing => listing.ViewCount,
                        listing => listing.ViewCount + 1),
                    cancellationToken);
        }

        return await dbContext.Listings
            .AsNoTracking()
            .Where(listing =>
                listing.Id == listingId)
            .Select(listing =>
                listing.ViewCount)
            .SingleAsync(cancellationToken);
    }
}