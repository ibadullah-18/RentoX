using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Messaging;

namespace RentoX.Domain.UnitTests.Messaging;

public sealed class UserBlockTests
{
    [Fact]
    public void BlockRetainsItsDirection()
    {
        Guid blocker = Guid.NewGuid();
        Guid target = Guid.NewGuid();
        UserBlock block = UserBlock.Create(blocker, target, DateTimeOffset.UtcNow);
        Assert.Equal(blocker, block.BlockerId);
        Assert.Equal(target, block.BlockedUserId);
        Assert.NotEqual(Guid.Empty, block.Id);
    }

    [Fact]
    public void SelfBlockIsRejected()
    {
        Guid userId = Guid.NewGuid();
        Assert.Throws<DomainException>(() => UserBlock.Create(userId, userId, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void MissingBlockerIsRejected()
    {
        Assert.Throws<DomainException>(() => UserBlock.Create(
            Guid.Empty, Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void MissingTargetIsRejected()
    {
        Assert.Throws<DomainException>(() => UserBlock.Create(
            Guid.NewGuid(), Guid.Empty, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void NonUtcTimeIsRejected()
    {
        DateTimeOffset localTime = new(2026, 9, 21, 12, 0, 0, TimeSpan.FromHours(4));
        Assert.Throws<DomainException>(() => UserBlock.Create(Guid.NewGuid(), Guid.NewGuid(), localTime));
    }
}
