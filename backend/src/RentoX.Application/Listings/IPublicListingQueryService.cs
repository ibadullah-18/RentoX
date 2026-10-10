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

    /// <summary>
    /// Listings like this one: the same brand first, then the same
    /// category, then the neighbouring categories.
    /// </summary>
    Task<IReadOnlyList<PublicListingSummaryResult>> GetSimilarAsync(
        Guid listingId,
        PreferredLanguage language,
        Guid? viewerUserId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<PublicListingDetailsResult?> GetByIdAsync(
        Guid listingId,
        PreferredLanguage language,
        Guid? viewerUserId,
        CancellationToken cancellationToken = default);
}