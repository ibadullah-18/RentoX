using RentoX.Application.Common;
using RentoX.Domain.Users.Enums;

namespace RentoX.Application.Listings;

public interface IPublicListingQueryService
{
    Task<PagedResult<PublicListingSummaryResult>>
        SearchAsync(
            PublicListingSearchQuery query,
            PreferredLanguage language,
            Guid? viewerUserId,
            CancellationToken cancellationToken = default);

    Task<PublicListingDetailsResult?> GetByIdAsync(
        Guid listingId,
        PreferredLanguage language,
        Guid? viewerUserId,
        CancellationToken cancellationToken = default);
}