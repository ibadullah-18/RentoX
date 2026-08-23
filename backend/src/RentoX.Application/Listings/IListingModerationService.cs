using RentoX.Application.Common;

namespace RentoX.Application.Listings;

public interface IListingModerationService
{
    Task<PagedResult<ModerationListingSummaryResult>>
        GetPendingAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

    Task<ListingModerationResult> ApproveAsync(
        Guid listingId,
        CancellationToken cancellationToken = default);

    Task<ListingModerationResult> RejectAsync(
        Guid listingId,
        string reason,
        CancellationToken cancellationToken = default);
}