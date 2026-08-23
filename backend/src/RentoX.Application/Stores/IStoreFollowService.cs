using RentoX.Application.Common;

namespace RentoX.Application.Stores;

public interface IStoreFollowService
{
    Task FollowAsync(
        Guid userId,
        Guid storeId,
        CancellationToken cancellationToken = default);

    Task UnfollowAsync(
        Guid userId,
        Guid storeId,
        CancellationToken cancellationToken = default);

    Task<StoreFollowStatusResult?> GetStatusAsync(
        Guid? userId,
        Guid storeId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<FollowedStoreResult>>
        GetFollowingAsync(
            Guid userId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);
}