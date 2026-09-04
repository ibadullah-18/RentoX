namespace RentoX.Contracts.Wallets;

public sealed record WalletOperationResponse(
    WalletBalanceResponse Wallet,
    WalletTransactionResponse Transaction,
    bool WasAlreadyProcessed);
