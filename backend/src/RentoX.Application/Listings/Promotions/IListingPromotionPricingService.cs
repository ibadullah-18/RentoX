using RentoX.Domain.Listings.Promotions.Enums;

namespace RentoX.Application.Listings.Promotions;

public interface IListingPromotionPricingService
{
    ListingPromotionPricingResult GetPricing(
        ListingPromotionType type);
}
