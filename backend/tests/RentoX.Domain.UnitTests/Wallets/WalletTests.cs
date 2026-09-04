using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Wallets;
using RentoX.Domain.Wallets.Enums;
using Xunit;

namespace RentoX.Domain.UnitTests.Wallets;

public sealed class WalletTests
{
    private static readonly DateTimeOffset OccurredAtUtc =
        new(
            2026,
            9,
            4,
            12,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public void CreateShouldInitializeEmptyAznWallet()
    {
        Guid userId = Guid.NewGuid();

        Wallet wallet = Wallet.Create(userId);

        Assert.Equal(userId, wallet.UserId);
        Assert.Equal(0, wallet.Balance);
        Assert.Equal("AZN", wallet.Currency);
        Assert.Empty(wallet.Transactions);
    }

    [Fact]
    public void CreditShouldIncreaseBalanceAndCreateLedgerEntry()
    {
        Wallet wallet =
            Wallet.Create(Guid.NewGuid());

        WalletTransaction transaction =
            wallet.Credit(
                25.50m,
                WalletTransactionType.TopUp,
                "Demo balance top-up",
                null,
                "topup-001",
                OccurredAtUtc);

        Assert.Equal(25.50m, wallet.Balance);
        Assert.Single(wallet.Transactions);
        Assert.Equal(0, transaction.BalanceBefore);
        Assert.Equal(25.50m, transaction.BalanceAfter);
        Assert.Equal(25.50m, transaction.Amount);

        Assert.Equal(
            WalletTransactionDirection.Credit,
            transaction.Direction);
    }

    [Fact]
    public void DebitShouldDecreaseBalanceAndCreateLedgerEntry()
    {
        Wallet wallet =
            Wallet.Create(Guid.NewGuid());

        wallet.Credit(
            20,
            WalletTransactionType.TopUp,
            "Demo balance top-up",
            null,
            "topup-001",
            OccurredAtUtc);

        Guid listingId = Guid.NewGuid();

        WalletTransaction transaction =
            wallet.Debit(
                1,
                WalletTransactionType.ListingFee,
                "Paid listing activation",
                listingId,
                "listing-fee-001",
                OccurredAtUtc.AddMinutes(1));

        Assert.Equal(19, wallet.Balance);
        Assert.Equal(20, transaction.BalanceBefore);
        Assert.Equal(19, transaction.BalanceAfter);
        Assert.Equal(listingId, transaction.RelatedEntityId);

        Assert.Equal(
            WalletTransactionDirection.Debit,
            transaction.Direction);
    }

    [Fact]
    public void DebitShouldRejectInsufficientBalance()
    {
        Wallet wallet =
            Wallet.Create(Guid.NewGuid());

        DomainException exception =
            Assert.Throws<DomainException>(() =>
                wallet.Debit(
                    1,
                    WalletTransactionType.ListingFee,
                    "Paid listing activation",
                    Guid.NewGuid(),
                    "listing-fee-001",
                    OccurredAtUtc));

        Assert.Equal(
            "Wallet balance is insufficient.",
            exception.Message);

        Assert.Equal(0, wallet.Balance);
        Assert.Empty(wallet.Transactions);
    }

    [Fact]
    public void DuplicateIdempotencyKeyShouldBeRejected()
    {
        Wallet wallet =
            Wallet.Create(Guid.NewGuid());

        wallet.Credit(
            10,
            WalletTransactionType.TopUp,
            "First top-up",
            null,
            "payment-001",
            OccurredAtUtc);

        Assert.Throws<DomainException>(() =>
            wallet.Credit(
                10,
                WalletTransactionType.TopUp,
                "Repeated top-up",
                null,
                "payment-001",
                OccurredAtUtc.AddSeconds(1)));

        Assert.Equal(10, wallet.Balance);
        Assert.Single(wallet.Transactions);
    }

    [Fact]
    public void ListingFeeCannotCreditWallet()
    {
        Wallet wallet =
            Wallet.Create(Guid.NewGuid());

        Assert.Throws<DomainException>(() =>
            wallet.Credit(
                1,
                WalletTransactionType.ListingFee,
                "Invalid credit",
                null,
                "invalid-credit-001",
                OccurredAtUtc));
    }

    [Fact]
    public void AmountWithMoreThanTwoDecimalsShouldBeRejected()
    {
        Wallet wallet =
            Wallet.Create(Guid.NewGuid());

        Assert.Throws<DomainException>(() =>
            wallet.Credit(
                1.001m,
                WalletTransactionType.TopUp,
                "Invalid amount",
                null,
                "invalid-amount-001",
                OccurredAtUtc));
    }
}
