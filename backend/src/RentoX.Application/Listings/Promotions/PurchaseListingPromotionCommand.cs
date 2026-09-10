namespace RentoX.Application.Listings.Promotions;

public sealed record PurchaseListingPromotionCommand(
    Guid OwnerId,
    Guid ListingId,
    int Type,
    string IdempotencyKey);
