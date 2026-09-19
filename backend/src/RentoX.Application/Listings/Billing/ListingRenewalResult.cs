namespace RentoX.Application.Listings.Billing;

public sealed record ListingRenewalResult(
    Guid ListingId,
    int Status,
    int CycleNumber,
    decimal ChargedAmount,
    decimal RemainingBalance,
    Guid? WalletTransactionId,
    DateTimeOffset PublishedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    bool WasAlreadyProcessed);
