using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Notifications;

public enum PushPlatform
{
    Android = 1,
    Ios = 2
}

public sealed class PushDevice : Entity
{
    private PushDevice() { }

    private PushDevice(
        Guid id,
        Guid userId,
        PushPlatform platform,
        string deviceId,
        string token,
        DateTimeOffset registeredAtUtc)
        : base(id)
    {
        UserId = RequireUser(userId);
        Platform = RequirePlatform(platform);
        DeviceId = Normalize(deviceId, 200, "Push device id");
        Token = Normalize(token, 2048, "Push token");
        RequireUtc(registeredAtUtc, "Push registration time");
        RegisteredAtUtc = registeredAtUtc;
        LastSeenAtUtc = registeredAtUtc;
        IsActive = true;
    }

    public Guid UserId { get; private set; }
    public PushPlatform Platform { get; private set; }
    public string DeviceId { get; private set; } = string.Empty;
    public string Token { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset RegisteredAtUtc { get; private set; }
    public DateTimeOffset LastSeenAtUtc { get; private set; }
    public DateTimeOffset? DeactivatedAtUtc { get; private set; }

    public static PushDevice Register(
        Guid userId,
        PushPlatform platform,
        string deviceId,
        string token,
        DateTimeOffset registeredAtUtc)
    {
        return new PushDevice(
            Guid.NewGuid(), userId, platform,
            deviceId, token, registeredAtUtc);
    }

    public void Refresh(
        Guid userId,
        PushPlatform platform,
        string deviceId,
        string token,
        DateTimeOffset seenAtUtc)
    {
        UserId = RequireUser(userId);
        Platform = RequirePlatform(platform);
        DeviceId = Normalize(deviceId, 200, "Push device id");
        Token = Normalize(token, 2048, "Push token");
        RequireUtc(seenAtUtc, "Push device last-seen time");
        LastSeenAtUtc = seenAtUtc;
        IsActive = true;
        DeactivatedAtUtc = null;
    }

    public void Deactivate(DateTimeOffset deactivatedAtUtc)
    {
        RequireUtc(deactivatedAtUtc, "Push deactivation time");
        IsActive = false;
        DeactivatedAtUtc ??= deactivatedAtUtc;
    }

    private static Guid RequireUser(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("Push device user id is required.");
        }
        return userId;
    }

    private static PushPlatform RequirePlatform(PushPlatform platform)
    {
        if (!Enum.IsDefined(platform))
        {
            throw new DomainException("Push platform is invalid.");
        }
        return platform;
    }

    private static string Normalize(string value, int maximumLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{field} is required.");
        }
        string normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new DomainException(
                $"{field} cannot exceed {maximumLength} characters.");
        }
        return normalized;
    }

    private static void RequireUtc(DateTimeOffset value, string field)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException($"{field} must be UTC.");
        }
    }
}
