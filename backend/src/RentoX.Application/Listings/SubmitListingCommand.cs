namespace RentoX.Application.Listings;

public sealed record SubmitListingCommand(
    Guid OwnerId,
    Guid ListingId);