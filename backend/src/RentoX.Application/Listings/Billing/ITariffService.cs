namespace RentoX.Application.Listings.Billing;

/// <summary>The prices in force: the free limit, the activation fee, the bump, VIP and its length.</summary>
public sealed record TariffValues(
    int FreeActiveListingLimit,
    decimal ActivationFee,
    decimal BumpPrice,
    decimal VipPrice,
    int VipDurationDays);

public sealed record TariffHistoryItem(
    Guid Id,
    TariffValues Values,
    Guid ChangedByUserId,
    DateTimeOffset ChangedAtUtc);

public interface ITariffService
{
    /// <summary>The newest version, or the starting prices when nobody has changed them yet.</summary>
    Task<TariffValues> GetCurrentAsync(CancellationToken cancellationToken = default);

    /// <summary>Puts a new version in force (an admin's action).</summary>
    Task<TariffValues> SetAsync(TariffValues values, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TariffHistoryItem>> GetHistoryAsync(int take, CancellationToken cancellationToken = default);
}
