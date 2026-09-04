namespace RentoX.Contracts.Wallets;

public sealed record DemoWalletTopUpRequest(
    decimal Amount,
    string IdempotencyKey);
