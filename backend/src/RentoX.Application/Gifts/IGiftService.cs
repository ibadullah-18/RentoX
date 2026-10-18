namespace RentoX.Application.Gifts;

/// <summary>What was given: who received it, from when and until when (null for a bump, which has no end).</summary>
public sealed record GiftResult(
    Guid RecipientUserId,
    string Title,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? EndsAtUtc);

/// <summary>
/// RentoX gives a VIP or a bump for free. Nothing is taken from the wallet of the person and nothing is added to the
/// income: there is no wallet transaction at all, only the promotion itself, marked as a gift, with who gave it.
/// </summary>
public interface IGiftService
{
    /// <param name="type">1 = bump, 2 = VIP.</param>
    /// <param name="days">VIP length; empty = the length of the paid VIP.</param>
    Task<GiftResult> GiftListingPromotionAsync(
        Guid actorUserId, Guid listingId, int type, int? days, CancellationToken cancellationToken = default);

    /// <summary>The services free for this person, as bits: 1 listing fee, 2 VIP, 4 bump, 8 VIP store.</summary>
    Task<int> GetFreeServicesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Chooses which paid services of a person are free, one by one (bits: 1 listing fee, 2 VIP, 4 bump, 8 VIP store;
    /// 0 = none). Nothing is taken from the wallet and nothing is counted as income. The person is told it is from RentoX.
    /// </summary>
    Task SetFreeServicesAsync(
        Guid actorUserId, Guid userId, int services, CancellationToken cancellationToken = default);

    /// <param name="days">Empty = the length of the VIP store plan.</param>
    Task<GiftResult> GiftStoreVipAsync(
        Guid actorUserId, Guid storeId, int? days, CancellationToken cancellationToken = default);
}
