namespace RentoX.Contracts.Listings;

public sealed record UpdateListingFieldsRequest(
    IReadOnlyList<CreateListingFieldRequest> Fields);