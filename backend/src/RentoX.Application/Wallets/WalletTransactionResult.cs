namespace RentoX.Application.Wallets;

public sealed record WalletTransactionResult(
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
