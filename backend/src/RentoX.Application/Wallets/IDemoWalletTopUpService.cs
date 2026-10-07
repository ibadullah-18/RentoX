namespace RentoX.Application.Wallets;

public interface IDemoWalletTopUpService
{
    Task<WalletOperationResult> TopUpAsync(
        decimal amount,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}
