using RentoX.Domain.Wallets.Enums;

namespace RentoX.Application.Wallets;

public sealed record CreditWalletCommand(
    Guid UserId,
    decimal Amount,
    WalletTransactionType Type,
    string Reason,
    Guid? RelatedEntityId,
    string IdempotencyKey);
