using RentoX.Application.Common;
using RentoX.Application.Listings;
using RentoX.Domain.Users.Enums;

namespace RentoX.Application.Stores;

public interface IPublicStoreQueryService
{
    Task<PublicStoreDetailsResult?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    Task<PagedResult<PublicListingSummaryResult>?>
        GetListingsAsync(
            string slug,
            PreferredLanguage language,
            Guid? viewerUserId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);
}