namespace RentoX.Application.Listings.Promotions;

public sealed record PurchaseListingPromotionResult(
    Guid PromotionId,
    Guid ListingId,
    int Type,
    decimal ChargedAmount,
    string Currency,
    decimal RemainingBalance,
    Guid WalletTransactionId,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? EndsAtUtc,
    bool WasAlreadyProcessed);
