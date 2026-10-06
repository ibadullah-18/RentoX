namespace RentoX.Application.Notifications;

public sealed record RegisterPushDeviceCommand(
    Guid UserId,
    int Platform,
    string DeviceId,
    string Token);

public sealed record PushDeviceResult(
    Guid Id,
    int Platform,
    string DeviceId,
    bool IsActive,
    DateTimeOffset RegisteredAtUtc,
    DateTimeOffset LastSeenAtUtc);

public interface IPushDeviceService
{
    Task<PushDeviceResult> RegisterAsync(
        RegisterPushDeviceCommand command,
        CancellationToken cancellationToken = default);

    Task<int> DeactivateAsync(
        Guid userId,
        string deviceId,
        CancellationToken cancellationToken = default);
}
