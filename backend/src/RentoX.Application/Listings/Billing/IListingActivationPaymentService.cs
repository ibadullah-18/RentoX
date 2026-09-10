namespace RentoX.Application.Listings.Billing;

public interface IListingActivationPaymentService
{
    Task<ListingActivationPaymentResult> PayAsync(
        PayListingActivationCommand command,
        CancellationToken cancellationToken = default);
}
