namespace RentoX.Application.Listings;

public sealed record UpdateListingFieldsCommand(
    Guid OwnerId,
    Guid ListingId,
    IReadOnlyList<CreateListingFieldInput> Fields);