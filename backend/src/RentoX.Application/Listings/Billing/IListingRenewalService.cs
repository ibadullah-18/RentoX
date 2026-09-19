namespace RentoX.Application.Listings.Billing;

public interface IListingRenewalService
{
    Task<ListingRenewalResult> RenewAsync(
        Guid ownerId,
        Guid listingId,
        CancellationToken cancellationToken = default);
}
