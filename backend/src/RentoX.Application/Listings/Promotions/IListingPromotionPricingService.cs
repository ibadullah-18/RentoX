using RentoX.Domain.Listings.Promotions.Enums;

namespace RentoX.Application.Listings.Promotions;

public interface IListingPromotionPricingService
{
    Task<ListingPromotionPricingResult> GetPricingAsync(
        ListingPromotionType type,
        CancellationToken cancellationToken = default);
}
