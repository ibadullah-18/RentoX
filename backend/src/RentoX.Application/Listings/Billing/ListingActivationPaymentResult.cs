namespace RentoX.Application.Listings.Billing;

public sealed record ListingActivationPaymentResult(
    Guid ListingId,
    int Status,
    decimal ChargedAmount,
    decimal RemainingBalance,
    Guid WalletTransactionId,
    DateTimeOffset PublishedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    bool WasAlreadyProcessed);
