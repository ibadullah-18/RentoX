using RentoX.Domain.Listings.Promotions.Enums;

namespace RentoX.Application.Listings.Promotions;

public sealed record ListingPromotionPricingResult(
    ListingPromotionType Type,
    decimal Price,
    string Currency,
    int? DurationDays);
