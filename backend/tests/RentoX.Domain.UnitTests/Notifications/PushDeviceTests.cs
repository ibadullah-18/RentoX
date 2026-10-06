using RentoX.Domain.Notifications;

namespace RentoX.Domain.UnitTests.Notifications;

public sealed class PushDeviceTests
{
    [Fact]
    public void RegisterCreatesActiveDevice()
    {
        PushDevice device = PushDevice.Register(
            Guid.NewGuid(), PushPlatform.Android,
            " device-1 ", " token-1 ", DateTimeOffset.UtcNow);
        Assert.True(device.IsActive);
        Assert.Equal("device-1", device.DeviceId);
        Assert.Equal("token-1", device.Token);
    }

    [Fact]
    public void RefreshReactivatesDeviceAndChangesOwner()
    {
        PushDevice device = PushDevice.Register(
            Guid.NewGuid(), PushPlatform.Android,
            "device-1", "token-1", DateTimeOffset.UtcNow);
        device.Deactivate(DateTimeOffset.UtcNow);
        Guid newUserId = Guid.NewGuid();
        device.Refresh(
            newUserId, PushPlatform.Ios,
            "device-2", "token-1", DateTimeOffset.UtcNow);
        Assert.True(device.IsActive);
        Assert.Equal(newUserId, device.UserId);
        Assert.Null(device.DeactivatedAtUtc);
    }

    [Fact]
    public void DeliveryStartsPending()
    {
        PushNotificationDelivery delivery =
            PushNotificationDelivery.Create(
                Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Equal(PushDeliveryStatus.Pending, delivery.Status);
        Assert.Equal(0, delivery.AttemptCount);
    }
}
