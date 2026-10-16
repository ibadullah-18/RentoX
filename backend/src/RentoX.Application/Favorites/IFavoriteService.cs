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

    /// <summary>Which of [listingIds] the user has favourited (one query).</summary>
    Task<IReadOnlySet<Guid>> GetFavoriteIdsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> listingIds,
        CancellationToken cancellationToken = default);

    Task<bool> IsFavoriteAsync(
        Guid userId,
        Guid listingId,
        CancellationToken cancellationToken = default);
}