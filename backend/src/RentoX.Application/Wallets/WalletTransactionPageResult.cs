namespace RentoX.Application.Wallets;

public sealed record WalletTransactionPageResult(
    IReadOnlyList<WalletTransactionResult> Items,
    long TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
