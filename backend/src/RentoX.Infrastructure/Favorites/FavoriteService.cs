using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Favorites;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Favorites;

public sealed class FavoriteService(
    RentoXDbContext dbContext,
    IClock clock)
    : IFavoriteService
{
    public async Task AddAsync(
        Guid userId,
        Guid listingId,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = clock.UtcNow;

        var listing =
            await dbContext.Listings
                .AsNoTracking()
                .Where(item =>
                    item.Id == listingId)
                .Select(item => new
                {
                    item.OwnerId,
                    item.Status,
                    item.ExpiresAtUtc
                })
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (listing is null ||
            listing.Status != ListingStatus.Active ||
            !listing.ExpiresAtUtc.HasValue ||
            listing.ExpiresAtUtc.Value <= now)
        {
            throw new DomainException(
                "Listing was not found.");
        }

        if (listing.OwnerId == userId)
        {
            throw new DomainException(
                "You cannot add your own listing to favorites.");
        }

        Guid favoriteId = Guid.NewGuid();

        await dbContext.Database
            .ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO engagement.favorites
                    ("Id", "UserId", "ListingId", "CreatedAtUtc")
                VALUES
                    ({favoriteId}, {userId}, {listingId}, {now})
                ON CONFLICT ("UserId", "ListingId")
                DO NOTHING
                """,
                cancellationToken);
    }

    public async Task RemoveAsync(
        Guid userId,
        Guid listingId,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Favorites
            .Where(favorite =>
                favorite.UserId == userId &&
                favorite.ListingId == listingId)
            .ExecuteDeleteAsync(
                cancellationToken);
    }

    public Task<bool> IsFavoriteAsync(
        Guid userId,
        Guid listingId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Favorites
            .AsNoTracking()
            .AnyAsync(
                favorite =>
                    favorite.UserId == userId &&
                    favorite.ListingId == listingId,
                cancellationToken);
    }
}