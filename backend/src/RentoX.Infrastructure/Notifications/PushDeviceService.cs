using Microsoft.EntityFrameworkCore;
using Npgsql;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Notifications;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Notifications;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Notifications;

public sealed class PushDeviceService(
    RentoXDbContext dbContext,
    IClock clock)
    : IPushDeviceService
{
    public async Task<PushDeviceResult> RegisterAsync(
        RegisterPushDeviceCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!Enum.IsDefined(typeof(PushPlatform), command.Platform))
        {
            throw new DomainException("Push platform is invalid.");
        }

        string token = Normalize(command.Token, "Push token");
        string deviceId = Normalize(command.DeviceId, "Push device id");
        DateTimeOffset now = clock.UtcNow;

        PushDevice? byToken = await dbContext.Set<PushDevice>()
            .SingleOrDefaultAsync(device => device.Token == token, cancellationToken);

        PushDevice device;
        if (byToken is not null)
        {
            device = byToken;
            device.Refresh(
                command.UserId,
                (PushPlatform)command.Platform,
                deviceId,
                token,
                now);
        }
        else
        {
            PushDevice[] previousDevices = await dbContext.Set<PushDevice>()
                .Where(item =>
                    item.UserId == command.UserId &&
                    item.DeviceId == deviceId &&
                    item.IsActive)
                .ToArrayAsync(cancellationToken);

            foreach (PushDevice previousDevice in previousDevices)
            {
                previousDevice.Deactivate(now);
            }

            device = PushDevice.Register(
                command.UserId,
                (PushPlatform)command.Platform,
                deviceId,
                token,
                now);
            dbContext.Set<PushDevice>().Add(device);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            })
        {
            throw new DomainException(
                "Push token registration conflicted with another request.");
        }

        return Map(device);
    }

    public async Task<int> DeactivateAsync(
        Guid userId,
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("Push device user id is required.");
        }
        string normalizedDeviceId = Normalize(deviceId, "Push device id");
        PushDevice[] devices = await dbContext.Set<PushDevice>()
            .Where(device =>
                device.UserId == userId &&
                device.DeviceId == normalizedDeviceId &&
                device.IsActive)
            .ToArrayAsync(cancellationToken);

        DateTimeOffset now = clock.UtcNow;
        foreach (PushDevice device in devices)
        {
            device.Deactivate(now);
        }
        if (devices.Length > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        return devices.Length;
    }

    private static PushDeviceResult Map(PushDevice device)
    {
        return new PushDeviceResult(
            device.Id,
            (int)device.Platform,
            device.DeviceId,
            device.IsActive,
            device.RegisteredAtUtc,
            device.LastSeenAtUtc);
    }

    private static string Normalize(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{field} is required.");
        }
        return value.Trim();
    }
}
