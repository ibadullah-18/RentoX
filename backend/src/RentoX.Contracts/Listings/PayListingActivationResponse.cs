namespace RentoX.Contracts.Listings;

public sealed record PayListingActivationResponse(
    Guid ListingId,
    int Status,
    decimal ChargedAmount,
    decimal RemainingBalance,
    Guid WalletTransactionId,
    DateTimeOffset PublishedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    bool WasAlreadyProcessed);
