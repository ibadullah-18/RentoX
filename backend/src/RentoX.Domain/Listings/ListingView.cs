using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Listings;

public sealed class ListingView
{
    private ListingView()
    {
    }

    private ListingView(
        Guid id,
        Guid listingId,
        Guid viewerUserId,
        DateTimeOffset viewedAtUtc)
    {
        if (listingId == Guid.Empty)
        {
            throw new DomainException(
                "Listing id is required.");
        }

        if (viewerUserId == Guid.Empty)
        {
            throw new DomainException(
                "Viewer user id is required.");
        }

        Id = id;
        ListingId = listingId;
        ViewerUserId = viewerUserId;
        ViewedAtUtc = viewedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid ListingId { get; private set; }

    public Guid ViewerUserId { get; private set; }

    public DateTimeOffset ViewedAtUtc { get; private set; }

    public static ListingView Create(
        Guid listingId,
        Guid viewerUserId,
        DateTimeOffset viewedAtUtc)
    {
        return new ListingView(
            Guid.NewGuid(),
            listingId,
            viewerUserId,
            viewedAtUtc);
    }
}