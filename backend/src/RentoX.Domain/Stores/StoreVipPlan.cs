using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Stores;

/// <summary>
/// The "VIP store" product (a paid place for a store on the home page), as one version: a change adds a new
/// version and the newest is in force. Nothing is edited or deleted.
/// </summary>
public sealed class StoreVipPlan : Entity
{
    public const int MaximumNameLength = 60;
    public const int MaximumMessageLength = 400;
    public const decimal MaximumPrice = 10000m;
    public const int MaximumDays = 365;
    public const int MaximumOfferIntervalHours = 24 * 90;

    private StoreVipPlan()
    {
    }

    private StoreVipPlan(
        Guid id, string name, decimal price, int durationDays, string offerMessage,
        int offerIntervalHours, bool isEnabled, Guid createdByUserId, DateTimeOffset createdAtUtc)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > MaximumNameLength)
        {
            throw new DomainException($"The name is required and at most {MaximumNameLength} characters.");
        }

        if (price < 0 || price > MaximumPrice || decimal.Round(price, 2) != price)
        {
            throw new DomainException($"The price must be between 0 and {MaximumPrice} with at most two decimals.");
        }

        if (durationDays is < 1 or > MaximumDays)
        {
            throw new DomainException($"The duration must be between 1 and {MaximumDays} days.");
        }

        string message = offerMessage?.Trim() ?? string.Empty;
        if (message.Length > MaximumMessageLength)
        {
            throw new DomainException($"The offer text must be at most {MaximumMessageLength} characters.");
        }

        if (offerIntervalHours is < 1 or > MaximumOfferIntervalHours)
        {
            throw new DomainException($"The offer interval must be between 1 and {MaximumOfferIntervalHours} hours.");
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new DomainException("The person who changed the plan is required.");
        }

        if (createdAtUtc == default || createdAtUtc.Offset != TimeSpan.Zero)
        {
            throw new DomainException("The change time must be a UTC time.");
        }

        Name = name.Trim();
        Price = price;
        DurationDays = durationDays;
        OfferMessage = message;
        OfferIntervalHours = offerIntervalHours;
        IsEnabled = isEnabled;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
    }

    public string Name { get; private set; } = string.Empty;

    public decimal Price { get; private set; }

    public int DurationDays { get; private set; }

    /// <summary>The text of the offer the store owner sees (and is notified with).</summary>
    public string OfferMessage { get; private set; } = string.Empty;

    /// <summary>The least time between two offers sent to the same owner.</summary>
    public int OfferIntervalHours { get; private set; }

    /// <summary>When off, the product cannot be bought and no offer is shown.</summary>
    public bool IsEnabled { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static StoreVipPlan Create(
        string name, decimal price, int durationDays, string offerMessage, int offerIntervalHours,
        bool isEnabled, Guid createdByUserId, DateTimeOffset createdAtUtc)
    {
        return new StoreVipPlan(
            Guid.NewGuid(), name, price, durationDays, offerMessage, offerIntervalHours, isEnabled,
            createdByUserId, createdAtUtc);
    }
}
