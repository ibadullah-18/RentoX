namespace RentoX.Contracts.Notifications;

public sealed record RegisterPushDeviceRequest(
    int Platform,
    string DeviceId,
    string Token);

public sealed record PushDeviceResponse(
    Guid Id,
    int Platform,
    string DeviceId,
    bool IsActive,
    DateTimeOffset RegisteredAtUtc,
    DateTimeOffset LastSeenAtUtc);

public sealed record DeactivatePushDeviceResponse(int UpdatedCount);
