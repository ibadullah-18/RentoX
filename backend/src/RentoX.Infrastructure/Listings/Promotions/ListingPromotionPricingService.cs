using RentoX.Application.Listings.Promotions;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings.Promotions.Enums;
using RentoX.Domain.Wallets;

namespace RentoX.Infrastructure.Listings.Promotions;

public sealed class ListingPromotionPricingService
    : IListingPromotionPricingService
{
    public const decimal BumpPrice = 5m;

    public const decimal VipPrice = 10m;

    public const int VipDurationDays = 7;

    public ListingPromotionPricingResult GetPricing(
        ListingPromotionType type)
    {
        return type switch
        {
            ListingPromotionType.Bump =>
                new ListingPromotionPricingResult(
                    type,
                    BumpPrice,
                    Wallet.SupportedCurrency,
                    null),

            ListingPromotionType.Vip =>
                new ListingPromotionPricingResult(
                    type,
                    VipPrice,
                    Wallet.SupportedCurrency,
                    VipDurationDays),

            _ => throw new DomainException(
                "Listing promotion type is invalid.")
        };
    }
}
