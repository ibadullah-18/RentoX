namespace RentoX.Application.Favorites;

public interface IFavoriteService
{
    Task AddAsync(
        Guid userId,
        Guid listingId,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        Guid userId,
        Guid listingId,
        CancellationToken cancellationToken = default);

    Task<bool> IsFavoriteAsync(
        Guid userId,
        Guid listingId,
        CancellationToken cancellationToken = default);
}