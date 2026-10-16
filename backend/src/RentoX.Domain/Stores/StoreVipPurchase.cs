using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Stores;

/// <summary>One payment for a VIP place: which store, what was paid, and the period it covers.</summary>
public sealed class StoreVipPurchase : Entity
{
    private StoreVipPurchase()
    {
    }

    private StoreVipPurchase(
        Guid id, Guid storeId, string planName, decimal chargedAmount, Guid walletTransactionId,
        DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc, DateTimeOffset createdAtUtc)
        : base(id)
    {
        if (storeId == Guid.Empty || walletTransactionId == Guid.Empty)
        {
            throw new DomainException("Store and payment ids are required.");
        }

        if (endsAtUtc <= startsAtUtc)
        {
            throw new DomainException("The VIP period must end after it starts.");
        }

        StoreId = storeId;
        PlanName = planName;
        ChargedAmount = chargedAmount;
        WalletTransactionId = walletTransactionId;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid StoreId { get; private set; }

    public string PlanName { get; private set; } = string.Empty;

    public decimal ChargedAmount { get; private set; }

    public Guid WalletTransactionId { get; private set; }

    public DateTimeOffset StartsAtUtc { get; private set; }

    public DateTimeOffset EndsAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static StoreVipPurchase Create(
        Guid storeId, string planName, decimal chargedAmount, Guid walletTransactionId,
        DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc, DateTimeOffset createdAtUtc)
    {
        return new StoreVipPurchase(
            Guid.NewGuid(), storeId, planName, chargedAmount, walletTransactionId, startsAtUtc, endsAtUtc,
            createdAtUtc);
    }
}
