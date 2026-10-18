namespace RentoX.Contracts.Stores;

/// <summary>The offer on the owner's store panel: the product, its price and length, and whether it is running.</summary>
public sealed record StoreVipStatusResponse(
    string Name,
    decimal Price,
    int DurationDays,
    string OfferMessage,
    bool IsOnSale,
    bool IsVip,
    DateTimeOffset? VipUntilUtc,
    string Currency,
    bool IsGift = false,
    bool FreeServices = false);

public sealed record StoreVipPurchaseRequest(string IdempotencyKey);

public sealed record StoreVipPurchaseResponse(
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    decimal ChargedAmount,
    decimal RemainingBalance,
    bool IsGift = false);
