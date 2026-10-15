using RentoX.Application.Common;

namespace RentoX.Application.Stores;

public interface IStoreModerationService
{
    Task<PagedResult<StoreProfileResult>>
        GetPendingAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

    Task<StoreProfileResult?> GetByIdAsync(
        Guid storeId,
        CancellationToken cancellationToken = default);

    Task<StoreStatusResult> ApproveAsync(
        Guid storeId,
        CancellationToken cancellationToken = default);

    Task<StoreStatusResult> RejectAsync(
        Guid storeId,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>Suspends a live store with a reason its owner is told.</summary>
    Task<StoreStatusResult> SuspendAsync(
        Guid storeId,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>Brings a suspended store back.</summary>
    Task<StoreStatusResult> ReinstateAsync(
        Guid storeId,
        CancellationToken cancellationToken = default);
}