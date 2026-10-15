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

    /// <summary>Takes a live, paused, pending or unpaid listing down with a reason the owner is told.</summary>
    Task<ListingModerationResult> TakeDownAsync(
        Guid listingId,
        string reason,
        CancellationToken cancellationToken = default);
}