using RentoX.Application.Listings.Billing;
using RentoX.Application.Listings.Promotions;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings.Promotions.Enums;
using RentoX.Domain.Wallets;

namespace RentoX.Infrastructure.Listings.Promotions;

public sealed class ListingPromotionPricingService(ITariffService tariffService)
    : IListingPromotionPricingService
{
    public async Task<ListingPromotionPricingResult> GetPricingAsync(
        ListingPromotionType type,
        CancellationToken cancellationToken = default)
    {
        TariffValues tariff = await tariffService.GetCurrentAsync(cancellationToken);

        return type switch
        {
            ListingPromotionType.Bump =>
                new ListingPromotionPricingResult(
                    type,
                    tariff.BumpPrice,
                    Wallet.SupportedCurrency,
                    null),

            ListingPromotionType.Vip =>
                new ListingPromotionPricingResult(
                    type,
                    tariff.VipPrice,
                    Wallet.SupportedCurrency,
                    tariff.VipDurationDays),

            _ => throw new DomainException(
                "Listing promotion type is invalid.")
        };
    }
}
