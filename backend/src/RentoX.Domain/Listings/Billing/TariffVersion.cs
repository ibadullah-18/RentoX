using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Listings.Billing;

/// <summary>
/// The prices of the site's paid services, as one version. A change adds a
/// new version (who, when); the newest one is in force and the older ones are
/// the history. Nothing is ever edited or deleted.
/// </summary>
public sealed class TariffVersion : Entity
{
    public const int MaximumFreeListings = 100;

    public const decimal MaximumPrice = 1000m;

    public const int MaximumVipDays = 90;

    private TariffVersion()
    {
    }

    private TariffVersion(
        Guid id,
        int freeActiveListingLimit,
        decimal activationFee,
        decimal bumpPrice,
        decimal vipPrice,
        int vipDurationDays,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        if (freeActiveListingLimit is < 0 or > MaximumFreeListings)
        {
            throw new DomainException(
                $"The free listing limit must be between 0 and {MaximumFreeListings}.");
        }

        RequirePrice(activationFee, "Activation fee");
        RequirePrice(bumpPrice, "Bump price");
        RequirePrice(vipPrice, "VIP price");

        if (vipDurationDays is < 1 or > MaximumVipDays)
        {
            throw new DomainException(
                $"The VIP duration must be between 1 and {MaximumVipDays} days.");
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new DomainException("The person who changed the prices is required.");
        }

        if (createdAtUtc == default || createdAtUtc.Offset != TimeSpan.Zero)
        {
            throw new DomainException("The change time must be a UTC time.");
        }

        FreeActiveListingLimit = freeActiveListingLimit;
        ActivationFee = activationFee;
        BumpPrice = bumpPrice;
        VipPrice = vipPrice;
        VipDurationDays = vipDurationDays;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
    }

    public int FreeActiveListingLimit { get; private set; }

    public decimal ActivationFee { get; private set; }

    public decimal BumpPrice { get; private set; }

    public decimal VipPrice { get; private set; }

    public int VipDurationDays { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static TariffVersion Create(
        int freeActiveListingLimit,
        decimal activationFee,
        decimal bumpPrice,
        decimal vipPrice,
        int vipDurationDays,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc)
    {
        return new TariffVersion(
            Guid.NewGuid(),
            freeActiveListingLimit,
            activationFee,
            bumpPrice,
            vipPrice,
            vipDurationDays,
            createdByUserId,
            createdAtUtc);
    }

    private static void RequirePrice(decimal value, string name)
    {
        if (value < 0 || value > MaximumPrice || decimal.Round(value, 2) != value)
        {
            throw new DomainException(
                $"{name} must be between 0 and {MaximumPrice} with at most two decimals.");
        }
    }
}
