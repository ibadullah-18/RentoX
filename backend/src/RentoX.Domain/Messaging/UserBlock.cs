using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Messaging;

public sealed class UserBlock : Entity
{
    private UserBlock() { }

    private UserBlock(Guid id, Guid blockerId, Guid blockedUserId, DateTimeOffset createdAtUtc)
        : base(id)
    {
        BlockerId = blockerId;
        BlockedUserId = blockedUserId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid BlockerId { get; private set; }
    public Guid BlockedUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static UserBlock Create(Guid blockerId, Guid blockedUserId, DateTimeOffset createdAtUtc)
    {
        if (blockerId == Guid.Empty || blockedUserId == Guid.Empty)
        {
            throw new DomainException("Both user ids are required.");
        }
        if (blockerId == blockedUserId)
        {
            throw new DomainException("You cannot block yourself.");
        }
        if (createdAtUtc.Offset != TimeSpan.Zero)
        {
            throw new DomainException("Block time must be UTC.");
        }
        return new UserBlock(Guid.NewGuid(), blockerId, blockedUserId, createdAtUtc);
    }
}
