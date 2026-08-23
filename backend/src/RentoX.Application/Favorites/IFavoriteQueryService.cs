using RentoX.Application.Common;
using RentoX.Application.Listings;
using RentoX.Domain.Users.Enums;

namespace RentoX.Application.Favorites;

public interface IFavoriteQueryService
{
    Task<PagedResult<PublicListingSummaryResult>>
        GetMineAsync(
            Guid userId,
            PreferredLanguage language,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);
}