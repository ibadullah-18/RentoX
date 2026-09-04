namespace RentoX.Contracts.Wallets;

public sealed record WalletTransactionPageResponse(
    IReadOnlyList<WalletTransactionResponse> Items,
    long TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
