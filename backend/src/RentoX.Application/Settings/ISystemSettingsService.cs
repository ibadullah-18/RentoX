namespace RentoX.Application.Settings;

/// <summary>The settings in force. Add a field here (and to the version) for the next adjustable value.</summary>
public sealed record SystemSettingsValues(int ListingLifetimeDays, int MaxListingPhotos = 15);

public sealed record SystemSettingsHistoryItem(
    Guid Id,
    SystemSettingsValues Values,
    Guid ChangedByUserId,
    DateTimeOffset ChangedAtUtc);

public interface ISystemSettingsService
{
    /// <summary>The newest version, or the starting values when nobody has changed them yet.</summary>
    Task<SystemSettingsValues> GetCurrentAsync(CancellationToken cancellationToken = default);

    /// <summary>Puts new settings in force (an admin's action).</summary>
    Task<SystemSettingsValues> SetAsync(SystemSettingsValues values, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SystemSettingsHistoryItem>> GetHistoryAsync(int take, CancellationToken cancellationToken = default);
}
