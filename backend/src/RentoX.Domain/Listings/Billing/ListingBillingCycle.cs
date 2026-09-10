using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings.Billing.Enums;

namespace RentoX.Domain.Listings.Billing;

public sealed class ListingBillingCycle : Entity
{
    private ListingBillingCycle()
    {
    }

    private ListingBillingCycle(
        Guid id,
        Guid listingId,
        int cycleNumber,
        ListingBillingCycleType type,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        decimal chargedAmount,
        Guid? walletTransactionId,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        if (listingId == Guid.Empty)
        {
            throw new DomainException(
                "Listing id is required.");
        }

        if (cycleNumber < 1)
        {
            throw new DomainException(
                "Billing cycle number must be positive.");
        }

        if (!Enum.IsDefined(type))
        {
            throw new DomainException(
                "Listing billing cycle type is invalid.");
        }

        if (periodStartUtc == default)
        {
            throw new DomainException(
                "Billing period start time is required.");
        }

        if (periodEndUtc <= periodStartUtc)
        {
            throw new DomainException(
                "Billing period end must be after its start.");
        }

        ValidateAmount(chargedAmount);

        if (chargedAmount == 0 &&
            walletTransactionId.HasValue)
        {
            throw new DomainException(
                "A free billing cycle cannot have a wallet transaction.");
        }

        if (chargedAmount > 0 &&
            (!walletTransactionId.HasValue ||
             walletTransactionId.Value == Guid.Empty))
        {
            throw new DomainException(
                "A paid billing cycle requires a wallet transaction.");
        }

        if (createdAtUtc == default)
        {
            throw new DomainException(
                "Billing cycle creation time is required.");
        }

        ListingId = listingId;
        CycleNumber = cycleNumber;
        Type = type;
        PeriodStartUtc = periodStartUtc;
        PeriodEndUtc = periodEndUtc;
        ChargedAmount = chargedAmount;
        WalletTransactionId = walletTransactionId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid ListingId { get; private set; }

    public int CycleNumber { get; private set; }

    public ListingBillingCycleType Type
    {
        get;
        private set;
    }

    public DateTimeOffset PeriodStartUtc
    {
        get;
        private set;
    }

    public DateTimeOffset PeriodEndUtc
    {
        get;
        private set;
    }

    public decimal ChargedAmount { get; private set; }

    public Guid? WalletTransactionId
    {
        get;
        private set;
    }

    public Guid? RefundWalletTransactionId
    {
        get;
        private set;
    }

    public DateTimeOffset? RefundedAtUtc
    {
        get;
        private set;
    }

    public DateTimeOffset CreatedAtUtc
    {
        get;
        private set;
    }

    public bool IsFree => ChargedAmount == 0;

    public bool IsRefunded =>
        RefundWalletTransactionId.HasValue;

    public static ListingBillingCycle CreateFree(
        Guid listingId,
        int cycleNumber,
        ListingBillingCycleType type,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        DateTimeOffset createdAtUtc)
    {
        return new ListingBillingCycle(
            Guid.NewGuid(),
            listingId,
            cycleNumber,
            type,
            periodStartUtc,
            periodEndUtc,
            0,
            null,
            createdAtUtc);
    }

    public static ListingBillingCycle CreatePaid(
        Guid listingId,
        int cycleNumber,
        ListingBillingCycleType type,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        decimal chargedAmount,
        Guid walletTransactionId,
        DateTimeOffset createdAtUtc)
    {
        return new ListingBillingCycle(
            Guid.NewGuid(),
            listingId,
            cycleNumber,
            type,
            periodStartUtc,
            periodEndUtc,
            chargedAmount,
            walletTransactionId,
            createdAtUtc);
    }

    public void MarkRefunded(
        Guid refundWalletTransactionId,
        DateTimeOffset refundedAtUtc)
    {
        if (IsFree)
        {
            throw new DomainException(
                "A free billing cycle cannot be refunded.");
        }

        if (IsRefunded)
        {
            throw new DomainException(
                "The billing cycle has already been refunded.");
        }

        if (refundWalletTransactionId == Guid.Empty)
        {
            throw new DomainException(
                "Refund wallet transaction id is required.");
        }

        if (refundedAtUtc == default)
        {
            throw new DomainException(
                "Refund time is required.");
        }

        RefundWalletTransactionId =
            refundWalletTransactionId;

        RefundedAtUtc = refundedAtUtc;
    }

    private static void ValidateAmount(
        decimal amount)
    {
        if (amount < 0)
        {
            throw new DomainException(
                "Billing amount cannot be negative.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new DomainException(
                "Billing amount cannot contain more than 2 decimal places.");
        }
    }
}
