namespace RentoX.Contracts.Wallets;

public sealed record WalletTransactionResponse(
    Guid Id,
    Guid WalletId,
    int Direction,
    int Type,
    decimal Amount,
    decimal BalanceBefore,
    decimal BalanceAfter,
    string Reason,
    Guid? RelatedEntityId,
    string IdempotencyKey,
    DateTimeOffset OccurredAtUtc);
