namespace RentoX.Application.Stores;

/// <summary>What the admin sets for the VIP store product.</summary>
public sealed record StoreVipPlanValues(
    string Name,
    decimal Price,
    int DurationDays,
    string OfferMessage,
    int OfferIntervalHours,
    bool IsEnabled);

/// <summary>What a store owner sees on the store panel: the offer and the current VIP period, if any.</summary>
public sealed record StoreVipStatusResult(
    StoreVipPlanValues Plan,
    bool IsVip,
    DateTimeOffset? VipUntilUtc,
    string Currency,
    /// <summary>The VIP period now running was given for free by RentoX.</summary>
    bool IsGift = false,
    /// <summary>RentoX pays for this owner: buying the VIP store costs nothing.</summary>
    bool FreeServices = false);

public sealed record StoreVipPurchaseResult(
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    decimal ChargedAmount,
    decimal RemainingBalance,
    bool IsGift = false);

public sealed record StoreVipOffersResult(int Sent, int SkippedRecently);

public interface IStoreVipService
{
    Task<StoreVipPlanValues> GetPlanAsync(CancellationToken cancellationToken = default);

    /// <summary>Puts a new version of the plan in force (an admin action).</summary>
    Task<StoreVipPlanValues> SetPlanAsync(StoreVipPlanValues values, CancellationToken cancellationToken = default);

    /// <summary>Null when the owner has no active store.</summary>
    Task<StoreVipStatusResult?> GetStatusAsync(Guid ownerId, CancellationToken cancellationToken = default);

    /// <summary>Pays from the wallet; buying again while VIP adds the new period after the current one.</summary>
    Task<StoreVipPurchaseResult> PurchaseAsync(Guid ownerId, string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>Notifies the owners of active stores that are not VIP (not those offered within the interval).</summary>
    Task<StoreVipOffersResult> SendOffersAsync(CancellationToken cancellationToken = default);

    /// <summary>The stores that are VIP now, for the home page.</summary>
    Task<IReadOnlyList<PublicStoreSummaryResult>> GetVipStoresAsync(int take, CancellationToken cancellationToken = default);
}
