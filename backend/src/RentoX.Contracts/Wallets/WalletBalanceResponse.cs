namespace RentoX.Contracts.Wallets;

public sealed record WalletBalanceResponse(
    Guid WalletId,
    Guid UserId,
    decimal Balance,
    string Currency);
