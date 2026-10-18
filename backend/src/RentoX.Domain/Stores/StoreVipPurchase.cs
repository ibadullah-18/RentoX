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
        Guid id, Guid storeId, string planName, decimal chargedAmount, Guid? walletTransactionId,
        DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc, DateTimeOffset createdAtUtc, Guid? grantedByUserId)
        : base(id)
    {
        if (storeId == Guid.Empty)
        {
            throw new DomainException("The store id is required.");
        }

        if (grantedByUserId.HasValue)
        {
            // A gift from RentoX: nothing is paid, so there is no wallet transaction and no income.
            if (grantedByUserId.Value == Guid.Empty || walletTransactionId.HasValue || chargedAmount != 0)
            {
                throw new DomainException("A gift is free, has no payment and needs the person who gave it.");
            }
        }
        else if (walletTransactionId is null || walletTransactionId == Guid.Empty)
        {
            throw new DomainException("The payment id is required.");
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
        GrantedByUserId = grantedByUserId;
    }

    public Guid StoreId { get; private set; }

    public string PlanName { get; private set; } = string.Empty;

    public decimal ChargedAmount { get; private set; }

    /// <summary>Null for a gift (nothing was paid).</summary>
    public Guid? WalletTransactionId { get; private set; }

    /// <summary>The staff member who gave this period for free; null when it was paid for.</summary>
    public Guid? GrantedByUserId { get; private set; }

    public bool IsGift => GrantedByUserId.HasValue;

    public DateTimeOffset StartsAtUtc { get; private set; }

    public DateTimeOffset EndsAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static StoreVipPurchase Create(
        Guid storeId, string planName, decimal chargedAmount, Guid walletTransactionId,
        DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc, DateTimeOffset createdAtUtc)
    {
        return new StoreVipPurchase(
            Guid.NewGuid(), storeId, planName, chargedAmount, walletTransactionId, startsAtUtc, endsAtUtc,
            createdAtUtc, null);
    }

    /// <summary>A VIP period given for free by RentoX.</summary>
    public static StoreVipPurchase CreateGift(
        Guid storeId, string planName, Guid grantedByUserId,
        DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc, DateTimeOffset createdAtUtc)
    {
        return new StoreVipPurchase(
            Guid.NewGuid(), storeId, planName, 0m, null, startsAtUtc, endsAtUtc, createdAtUtc, grantedByUserId);
    }
}
