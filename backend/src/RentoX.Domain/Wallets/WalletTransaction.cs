using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Wallets.Enums;

namespace RentoX.Domain.Wallets;

public sealed class WalletTransaction : Entity
{
    public const int MaximumReasonLength = 500;

    public const int MaximumIdempotencyKeyLength = 200;

    private WalletTransaction()
    {
    }

    private WalletTransaction(
        Guid id,
        Guid walletId,
        WalletTransactionDirection direction,
        WalletTransactionType type,
        decimal amount,
        decimal balanceBefore,
        decimal balanceAfter,
        string reason,
        Guid? relatedEntityId,
        string idempotencyKey,
        DateTimeOffset occurredAtUtc)
        : base(id)
    {
        if (walletId == Guid.Empty)
        {
            throw new DomainException(
                "Wallet id is required.");
        }

        if (!Enum.IsDefined(direction))
        {
            throw new DomainException(
                "Wallet transaction direction is invalid.");
        }

        if (!Enum.IsDefined(type))
        {
            throw new DomainException(
                "Wallet transaction type is invalid.");
        }

        ValidateAmount(amount);

        if (balanceBefore < 0 ||
            balanceAfter < 0)
        {
            throw new DomainException(
                "Wallet balance cannot be negative.");
        }

        decimal expectedBalance =
            direction ==
            WalletTransactionDirection.Credit
                ? balanceBefore + amount
                : balanceBefore - amount;

        if (expectedBalance != balanceAfter)
        {
            throw new DomainException(
                "Wallet transaction balance is invalid.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                "Wallet transaction reason is required.");
        }

        string normalizedReason = reason.Trim();

        if (normalizedReason.Length >
            MaximumReasonLength)
        {
            throw new DomainException(
                "Wallet transaction reason is too long.");
        }

        if (relatedEntityId == Guid.Empty)
        {
            throw new DomainException(
                "Related entity id cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(
                idempotencyKey))
        {
            throw new DomainException(
                "Idempotency key is required.");
        }

        string normalizedIdempotencyKey =
            idempotencyKey.Trim();

        if (normalizedIdempotencyKey.Length >
            MaximumIdempotencyKeyLength)
        {
            throw new DomainException(
                "Idempotency key is too long.");
        }

        if (occurredAtUtc == default)
        {
            throw new DomainException(
                "Transaction occurrence time is required.");
        }

        WalletId = walletId;
        Direction = direction;
        Type = type;
        Amount = amount;
        BalanceBefore = balanceBefore;
        BalanceAfter = balanceAfter;
        Reason = normalizedReason;
        RelatedEntityId = relatedEntityId;
        IdempotencyKey = normalizedIdempotencyKey;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid WalletId { get; private set; }

    public WalletTransactionDirection Direction
    {
        get;
        private set;
    }

    public WalletTransactionType Type
    {
        get;
        private set;
    }

    public decimal Amount { get; private set; }

    public decimal BalanceBefore { get; private set; }

    public decimal BalanceAfter { get; private set; }

    public string Reason { get; private set; } =
        string.Empty;

    public Guid? RelatedEntityId { get; private set; }

    public string IdempotencyKey { get; private set; } =
        string.Empty;

    public DateTimeOffset OccurredAtUtc
    {
        get;
        private set;
    }

    internal static WalletTransaction Create(
        Guid walletId,
        WalletTransactionDirection direction,
        WalletTransactionType type,
        decimal amount,
        decimal balanceBefore,
        decimal balanceAfter,
        string reason,
        Guid? relatedEntityId,
        string idempotencyKey,
        DateTimeOffset occurredAtUtc)
    {
        return new WalletTransaction(
            Guid.NewGuid(),
            walletId,
            direction,
            type,
            amount,
            balanceBefore,
            balanceAfter,
            reason,
            relatedEntityId,
            idempotencyKey,
            occurredAtUtc);
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new DomainException(
                "Wallet transaction amount must be positive.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new DomainException(
                "Wallet transaction amount cannot contain more than 2 decimal places.");
        }
    }
}
