using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Wallets.Enums;

namespace RentoX.Domain.Wallets;

public sealed class Wallet : AuditableEntity
{
    public const string SupportedCurrency = "AZN";

    private readonly List<WalletTransaction>
        _transactions = [];

    private Wallet()
    {
    }

    private Wallet(
        Guid id,
        Guid userId)
        : base(id)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException(
                "Wallet user id is required.");
        }

        UserId = userId;
        Currency = SupportedCurrency;
        Balance = 0;
    }

    public Guid UserId { get; private set; }

    public decimal Balance { get; private set; }

    public string Currency { get; private set; } =
        SupportedCurrency;

    public uint Version { get; private set; }

    public IReadOnlyCollection<WalletTransaction>
        Transactions =>
            _transactions.AsReadOnly();

    public static Wallet Create(Guid userId)
    {
        return new Wallet(
            Guid.NewGuid(),
            userId);
    }

    public WalletTransaction Credit(
        decimal amount,
        WalletTransactionType type,
        string reason,
        Guid? relatedEntityId,
        string idempotencyKey,
        DateTimeOffset occurredAtUtc)
    {
        if (type is not
            (WalletTransactionType.TopUp or
             WalletTransactionType.Refund or
             WalletTransactionType.Bonus or
             WalletTransactionType.AdminAdjustment))
        {
            throw new DomainException(
                "Transaction type cannot credit a wallet.");
        }

        ValidateAmount(amount);

        string normalizedIdempotencyKey =
            NormalizeIdempotencyKey(idempotencyKey);

        EnsureUniqueIdempotencyKey(
            normalizedIdempotencyKey);

        decimal balanceBefore = Balance;
        decimal balanceAfter = balanceBefore + amount;

        WalletTransaction transaction =
            WalletTransaction.Create(
                Id,
                WalletTransactionDirection.Credit,
                type,
                amount,
                balanceBefore,
                balanceAfter,
                reason,
                relatedEntityId,
                normalizedIdempotencyKey,
                occurredAtUtc);

        Balance = balanceAfter;
        _transactions.Add(transaction);

        return transaction;
    }

    public WalletTransaction Debit(
        decimal amount,
        WalletTransactionType type,
        string reason,
        Guid? relatedEntityId,
        string idempotencyKey,
        DateTimeOffset occurredAtUtc)
    {
        if (type is not
            (WalletTransactionType.ListingFee or
             WalletTransactionType.PromotionFee or
             WalletTransactionType.AdminAdjustment))
        {
            throw new DomainException(
                "Transaction type cannot debit a wallet.");
        }

        ValidateAmount(amount);

        string normalizedIdempotencyKey =
            NormalizeIdempotencyKey(idempotencyKey);

        EnsureUniqueIdempotencyKey(
            normalizedIdempotencyKey);

        if (Balance < amount)
        {
            throw new DomainException(
                "Wallet balance is insufficient.");
        }

        decimal balanceBefore = Balance;
        decimal balanceAfter = balanceBefore - amount;

        WalletTransaction transaction =
            WalletTransaction.Create(
                Id,
                WalletTransactionDirection.Debit,
                type,
                amount,
                balanceBefore,
                balanceAfter,
                reason,
                relatedEntityId,
                normalizedIdempotencyKey,
                occurredAtUtc);

        Balance = balanceAfter;
        _transactions.Add(transaction);

        return transaction;
    }

    private void EnsureUniqueIdempotencyKey(
        string idempotencyKey)
    {
        if (_transactions.Any(transaction =>
                transaction.IdempotencyKey ==
                idempotencyKey))
        {
            throw new DomainException(
                "A wallet transaction with this idempotency key already exists.");
        }
    }

    private static string NormalizeIdempotencyKey(
        string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(
                idempotencyKey))
        {
            throw new DomainException(
                "Idempotency key is required.");
        }

        string normalized =
            idempotencyKey.Trim();

        if (normalized.Length >
            WalletTransaction
                .MaximumIdempotencyKeyLength)
        {
            throw new DomainException(
                "Idempotency key is too long.");
        }

        return normalized;
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new DomainException(
                "Wallet amount must be positive.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new DomainException(
                "Wallet amount cannot contain more than 2 decimal places.");
        }
    }
}
