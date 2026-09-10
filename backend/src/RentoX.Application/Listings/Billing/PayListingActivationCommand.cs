namespace RentoX.Application.Listings.Billing;

public sealed record PayListingActivationCommand(
    Guid OwnerId,
    Guid ListingId);
