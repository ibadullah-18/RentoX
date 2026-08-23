using RentoX.Domain.Users.Enums;

namespace RentoX.Application.Listings;

public interface IListingModerationQueryService
{
    Task<ModerationListingDetailsResult?> GetByIdAsync(
        Guid listingId,
        PreferredLanguage language,
        CancellationToken cancellationToken = default);
}