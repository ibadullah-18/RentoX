namespace RentoX.Application.Listings.Billing;

public interface IListingActivationPricingService
{
    Task<ListingActivationPricingResult> GetAsync(
        Guid ownerId,
        Guid listingId,
        CancellationToken cancellationToken = default);
}
