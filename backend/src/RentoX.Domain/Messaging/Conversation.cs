using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Messaging;

public sealed class Conversation : Entity
{
    private Conversation()
    {
    }

    private Conversation(
        Guid id,
        Guid listingId,
        Guid buyerId,
        Guid sellerId,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        if (listingId == Guid.Empty ||
            buyerId == Guid.Empty ||
            sellerId == Guid.Empty)
        {
            throw new DomainException(
                "Conversation ids are required.");
        }

        if (buyerId == sellerId)
        {
            throw new DomainException(
                "A listing owner cannot start a conversation with themselves.");
        }

        if (createdAtUtc.Offset != TimeSpan.Zero)
        {
            throw new DomainException(
                "Conversation time must be UTC.");
        }

        ListingId = listingId;
        BuyerId = buyerId;
        SellerId = sellerId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid ListingId { get; private set; }

    public Guid BuyerId { get; private set; }

    public Guid SellerId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Conversation Create(
        Guid listingId,
        Guid buyerId,
        Guid sellerId,
        DateTimeOffset createdAtUtc)
    {
        return new Conversation(
            Guid.NewGuid(),
            listingId,
            buyerId,
            sellerId,
            createdAtUtc);
    }
}