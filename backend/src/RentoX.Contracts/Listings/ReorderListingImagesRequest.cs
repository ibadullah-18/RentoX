namespace RentoX.Contracts.Listings;

public sealed record ReorderListingImagesRequest(
    IReadOnlyList<Guid> ImageIds);