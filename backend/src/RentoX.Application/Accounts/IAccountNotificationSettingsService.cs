namespace RentoX.Application.Accounts;

/// <summary>What a person chose to receive.</summary>
public interface IAccountNotificationSettingsService
{
    /// <returns>Null when the account does not exist.</returns>
    Task<bool?> GetAnnouncementsEnabledAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <returns>False when the account does not exist.</returns>
    Task<bool> SetAnnouncementsEnabledAsync(Guid userId, bool enabled, CancellationToken cancellationToken = default);
}
