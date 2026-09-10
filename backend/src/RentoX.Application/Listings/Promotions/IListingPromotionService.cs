namespace RentoX.Application.Listings.Promotions;

public interface IListingPromotionService
{
    Task<PurchaseListingPromotionResult>
        PurchaseAsync(
            PurchaseListingPromotionCommand command,
            CancellationToken cancellationToken = default);
}
