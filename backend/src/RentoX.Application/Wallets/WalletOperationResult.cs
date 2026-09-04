namespace RentoX.Application.Wallets;

public sealed record WalletOperationResult(
    WalletBalanceResult Wallet,
    WalletTransactionResult Transaction,
    bool WasAlreadyProcessed);
