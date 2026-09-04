namespace RentoX.Application.Wallets;

public interface IWalletService
{
    Task<WalletBalanceResult> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<WalletOperationResult> CreditAsync(
        CreditWalletCommand command,
        CancellationToken cancellationToken = default);

    Task<WalletOperationResult> DebitAsync(
        DebitWalletCommand command,
        CancellationToken cancellationToken = default);

    Task<WalletTransactionPageResult>
        GetTransactionsAsync(
            Guid userId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);
}
