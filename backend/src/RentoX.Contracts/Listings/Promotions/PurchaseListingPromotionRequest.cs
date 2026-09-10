namespace RentoX.Contracts.Listings.Promotions;

public sealed record PurchaseListingPromotionRequest(
    int Type,
    string IdempotencyKey);
