using RentoX.Application.Common;
using RentoX.Domain.Users.Enums;

namespace RentoX.Application.Listings;

public interface IListingQueryService
{
    Task<PagedResult<OwnedListingSummaryResult>>
        GetMineAsync(
            Guid ownerId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

    Task<OwnedListingDetailsResult?> GetMineByIdAsync(
        Guid ownerId,
        Guid listingId,
        PreferredLanguage language,
        CancellationToken cancellationToken = default);
}