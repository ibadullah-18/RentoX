using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Favorites;

public sealed class Favorite
{
    private Favorite()
    {
    }

    private Favorite(
        Guid id,
        Guid userId,
        Guid listingId,
        DateTimeOffset createdAtUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException(
                "User id is required.");
        }

        if (listingId == Guid.Empty)
        {
            throw new DomainException(
                "Listing id is required.");
        }

        Id = id;
        UserId = userId;
        ListingId = listingId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid ListingId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Favorite Create(
        Guid userId,
        Guid listingId,
        DateTimeOffset createdAtUtc)
    {
        return new Favorite(
            Guid.NewGuid(),
            userId,
            listingId,
            createdAtUtc);
    }
}