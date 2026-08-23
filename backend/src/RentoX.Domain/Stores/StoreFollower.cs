using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Stores;

public sealed class StoreFollower : Entity
{
    private StoreFollower()
    {
    }

    private StoreFollower(
        Guid id,
        Guid userId,
        Guid storeId,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        UserId = userId;
        StoreId = storeId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid UserId { get; private set; }

    public Guid StoreId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static StoreFollower Create(
        Guid userId,
        Guid storeId,
        DateTimeOffset createdAtUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException(
                "User ID cannot be empty.");
        }

        if (storeId == Guid.Empty)
        {
            throw new DomainException(
                "Store ID cannot be empty.");
        }

        return new StoreFollower(
            Guid.NewGuid(),
            userId,
            storeId,
            createdAtUtc);
    }
}