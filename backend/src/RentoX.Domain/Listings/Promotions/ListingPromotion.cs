using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings.Promotions.Enums;

namespace RentoX.Domain.Listings.Promotions;

public sealed class ListingPromotion : Entity
{
    private ListingPromotion()
    {
    }

    private ListingPromotion(
        Guid id,
        Guid listingId,
        ListingPromotionType type,
        decimal chargedAmount,
        Guid? walletTransactionId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset? endsAtUtc,
        DateTimeOffset purchasedAtUtc,
        Guid? grantedByUserId)
        : base(id)
    {
        if (listingId == Guid.Empty)
        {
            throw new DomainException(
                "Listing id is required.");
        }

        if (!Enum.IsDefined(type))
        {
            throw new DomainException(
                "Listing promotion type is invalid.");
        }

        bool isGift = grantedByUserId.HasValue;

        if (isGift)
        {
            // A gift from RentoX: nothing is paid, so there is no wallet transaction and no income.
            if (grantedByUserId!.Value == Guid.Empty ||
                walletTransactionId.HasValue ||
                chargedAmount != 0)
            {
                throw new DomainException(
                    "A gift is free, has no payment and needs the person who gave it.");
            }
        }
        else
        {
            ValidateAmount(chargedAmount);

            if (walletTransactionId is null || walletTransactionId == Guid.Empty)
            {
                throw new DomainException(
                    "Wallet transaction id is required.");
            }
        }

        if (startsAtUtc == default)
        {
            throw new DomainException(
                "Promotion start time is required.");
        }

        if (purchasedAtUtc == default)
        {
            throw new DomainException(
                "Promotion purchase time is required.");
        }

        if (type == ListingPromotionType.Bump &&
            endsAtUtc.HasValue)
        {
            throw new DomainException(
                "Bump promotion cannot have an end time.");
        }

        if (type == ListingPromotionType.Vip &&
            (!endsAtUtc.HasValue ||
             endsAtUtc.Value <= startsAtUtc))
        {
            throw new DomainException(
                "VIP promotion end time must be after its start.");
        }

        ListingId = listingId;
        Type = type;
        ChargedAmount = chargedAmount;
        WalletTransactionId = walletTransactionId;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        PurchasedAtUtc = purchasedAtUtc;
        GrantedByUserId = grantedByUserId;
    }

    public Guid ListingId { get; private set; }

    public ListingPromotionType Type
    {
        get;
        private set;
    }

    public decimal ChargedAmount { get; private set; }

    /// <summary>Null for a gift (nothing was paid).</summary>
    public Guid? WalletTransactionId
    {
        get;
        private set;
    }

    /// <summary>The staff member who gave this promotion for free; null when it was paid for.</summary>
    public Guid? GrantedByUserId { get; private set; }

    public bool IsGift => GrantedByUserId.HasValue;

    public DateTimeOffset StartsAtUtc
    {
        get;
        private set;
    }

    public DateTimeOffset? EndsAtUtc
    {
        get;
        private set;
    }

    public DateTimeOffset PurchasedAtUtc
    {
        get;
        private set;
    }

    public bool IsBump =>
        Type == ListingPromotionType.Bump;

    public bool IsVipActiveAt(
        DateTimeOffset utcNow)
    {
        return Type == ListingPromotionType.Vip &&
               StartsAtUtc <= utcNow &&
               EndsAtUtc.HasValue &&
               EndsAtUtc.Value > utcNow;
    }

    public static ListingPromotion CreateBump(
        Guid listingId,
        decimal chargedAmount,
        Guid walletTransactionId,
        DateTimeOffset occurredAtUtc)
    {
        return new ListingPromotion(
            Guid.NewGuid(),
            listingId,
            ListingPromotionType.Bump,
            chargedAmount,
            walletTransactionId,
            occurredAtUtc,
            null,
            occurredAtUtc,
            null);
    }

    /// <summary>A bump given for free by RentoX.</summary>
    public static ListingPromotion CreateGiftBump(
        Guid listingId,
        Guid grantedByUserId,
        DateTimeOffset occurredAtUtc)
    {
        return new ListingPromotion(
            Guid.NewGuid(),
            listingId,
            ListingPromotionType.Bump,
            0m,
            null,
            occurredAtUtc,
            null,
            occurredAtUtc,
            grantedByUserId);
    }

    /// <summary>A VIP period given for free by RentoX.</summary>
    public static ListingPromotion CreateGiftVip(
        Guid listingId,
        Guid grantedByUserId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        DateTimeOffset occurredAtUtc)
    {
        return new ListingPromotion(
            Guid.NewGuid(),
            listingId,
            ListingPromotionType.Vip,
            0m,
            null,
            startsAtUtc,
            endsAtUtc,
            occurredAtUtc,
            grantedByUserId);
    }

    public static ListingPromotion CreateVip(
        Guid listingId,
        decimal chargedAmount,
        Guid walletTransactionId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        DateTimeOffset purchasedAtUtc)
    {
        return new ListingPromotion(
            Guid.NewGuid(),
            listingId,
            ListingPromotionType.Vip,
            chargedAmount,
            walletTransactionId,
            startsAtUtc,
            endsAtUtc,
            purchasedAtUtc,
            null);
    }

    private static void ValidateAmount(
        decimal amount)
    {
        if (amount <= 0)
        {
            throw new DomainException(
                "Promotion amount must be positive.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new DomainException(
                "Promotion amount cannot contain more than 2 decimal places.");
        }
    }
}
