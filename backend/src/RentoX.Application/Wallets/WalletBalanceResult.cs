namespace RentoX.Application.Wallets;

public sealed record WalletBalanceResult(
    Guid WalletId,
    Guid UserId,
    decimal Balance,
    string Currency);
