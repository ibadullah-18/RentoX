using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Listings;

public sealed class ListingFieldSelection : Entity
{
    private ListingFieldSelection()
    {
    }

    private ListingFieldSelection(
        Guid id,
        Guid listingFieldValueId,
        Guid categoryFieldOptionId)
        : base(id)
    {
        ListingFieldValueId =
            listingFieldValueId;

        CategoryFieldOptionId =
            categoryFieldOptionId;
    }

    public Guid ListingFieldValueId { get; private set; }

    public Guid CategoryFieldOptionId { get; private set; }

    public static ListingFieldSelection Create(
        Guid listingFieldValueId,
        Guid categoryFieldOptionId)
    {
        if (listingFieldValueId == Guid.Empty)
        {
            throw new DomainException(
                "Listing field value id is required.");
        }

        if (categoryFieldOptionId == Guid.Empty)
        {
            throw new DomainException(
                "Category field option id is required.");
        }

        return new ListingFieldSelection(
            Guid.NewGuid(),
            listingFieldValueId,
            categoryFieldOptionId);
    }
}