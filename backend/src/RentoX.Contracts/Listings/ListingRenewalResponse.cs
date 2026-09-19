namespace RentoX.Contracts.Listings;

public sealed record ListingRenewalResponse(
    Guid ListingId,
    int Status,
    int CycleNumber,
    decimal ChargedAmount,
    decimal RemainingBalance,
    Guid? WalletTransactionId,
    DateTimeOffset PublishedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    bool WasAlreadyProcessed);
