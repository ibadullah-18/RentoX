using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Settings;

/// <summary>
/// The site's adjustable settings, as one version (like the prices): a change adds a
/// new version (who, when); the newest is in force and the older ones are the history.
/// </summary>
public sealed class SettingsVersion : Entity
{
    public const int MinimumListingLifetimeDays = 7;

    public const int MaximumListingLifetimeDays = 365;

    public const int MinimumListingPhotos = 1;

    /// <summary>The most photos an admin may allow on one listing.</summary>
    public const int MaximumListingPhotos = 30;

    public const int DefaultListingPhotos = 15;

    /// <summary>The most new listings one person may create in a day (0: no limit).</summary>
    public const int MaximumNewListingsPerDay = 200;

    private SettingsVersion()
    {
    }

    private SettingsVersion(
        Guid id,
        int listingLifetimeDays,
        int maxListingPhotos,
        int minListingPhotos,
        int maxNewListingsPerDay,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        if (minListingPhotos < 0 || minListingPhotos > maxListingPhotos)
        {
            throw new DomainException("The least number of photos cannot be more than the most.");
        }

        if (maxNewListingsPerDay is < 0 or > MaximumNewListingsPerDay)
        {
            throw new DomainException($"The daily limit of new listings must be between 0 and {MaximumNewListingsPerDay}.");
        }

        if (maxListingPhotos is < MinimumListingPhotos or > MaximumListingPhotos)
        {
            throw new DomainException(
                $"The number of photos on a listing must be between {MinimumListingPhotos} and {MaximumListingPhotos}.");
        }

        if (listingLifetimeDays is < MinimumListingLifetimeDays or > MaximumListingLifetimeDays)
        {
            throw new DomainException(
                $"The listing lifetime must be between {MinimumListingLifetimeDays} and {MaximumListingLifetimeDays} days.");
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new DomainException("The person who changed the settings is required.");
        }

        if (createdAtUtc == default || createdAtUtc.Offset != TimeSpan.Zero)
        {
            throw new DomainException("The change time must be a UTC time.");
        }

        ListingLifetimeDays = listingLifetimeDays;
        MaxListingPhotos = maxListingPhotos;
        MinListingPhotos = minListingPhotos;
        MaxNewListingsPerDay = maxNewListingsPerDay;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>How many days a listing stays on show after it is published or renewed.</summary>
    public int ListingLifetimeDays { get; private set; }

    /// <summary>How many photos one listing may have.</summary>
    public int MaxListingPhotos { get; private set; }

    /// <summary>How many photos a listing needs before it can be sent for review (0: none needed).</summary>
    public int MinListingPhotos { get; private set; }

    /// <summary>How many new listings one person may create in a day (0: no limit).</summary>
    public int MaxNewListingsPerDay { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static SettingsVersion Create(
        int listingLifetimeDays,
        int maxListingPhotos,
        int minListingPhotos,
        int maxNewListingsPerDay,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc) =>
        new(Guid.NewGuid(), listingLifetimeDays, maxListingPhotos, minListingPhotos, maxNewListingsPerDay, createdByUserId, createdAtUtc);
}
