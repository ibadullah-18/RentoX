namespace RentoX.Application.Listings.Billing;

public sealed record ListingActivationPricingResult(
    Guid ListingId,
    Guid OwnerId,
    int OtherActiveListingCount,
    int FreeActiveListingLimit,
    bool RequiresPayment,
    decimal ActivationFee,
    string Currency);
