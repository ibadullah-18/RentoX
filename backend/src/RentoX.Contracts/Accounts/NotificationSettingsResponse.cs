namespace RentoX.Contracts.Accounts;

/// <param name="AnnouncementsEnabled">Whether messages the staff send to groups reach the person.</param>
public sealed record NotificationSettingsResponse(bool AnnouncementsEnabled);
