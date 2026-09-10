using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings.Billing;
using RentoX.Domain.Listings.Billing.Enums;
using Xunit;

namespace RentoX.Domain.UnitTests.Listings.Billing;

public sealed class ListingBillingCycleTests
{
    private static readonly DateTimeOffset PeriodStart =
        new(
            2026,
            9,
            6,
            0,
            0,
            0,
            TimeSpan.Zero);

    private static readonly DateTimeOffset PeriodEnd =
        PeriodStart.AddDays(30);

    [Fact]
    public void FreeCycleShouldNotRequireWalletTransaction()
    {
        ListingBillingCycle cycle =
            ListingBillingCycle.CreateFree(
                Guid.NewGuid(),
                1,
                ListingBillingCycleType.InitialActivation,
                PeriodStart,
                PeriodEnd,
                PeriodStart);

        Assert.True(cycle.IsFree);
        Assert.Equal(0, cycle.ChargedAmount);
        Assert.Null(cycle.WalletTransactionId);
        Assert.False(cycle.IsRefunded);
    }

    [Fact]
    public void PaidCycleShouldKeepWalletTransaction()
    {
        Guid walletTransactionId =
            Guid.NewGuid();

        ListingBillingCycle cycle =
            ListingBillingCycle.CreatePaid(
                Guid.NewGuid(),
                1,
                ListingBillingCycleType.InitialActivation,
                PeriodStart,
                PeriodEnd,
                1,
                walletTransactionId,
                PeriodStart);

        Assert.False(cycle.IsFree);
        Assert.Equal(1, cycle.ChargedAmount);

        Assert.Equal(
            walletTransactionId,
            cycle.WalletTransactionId);
    }

    [Fact]
    public void PaidCycleCanBeRefundedOnce()
    {
        ListingBillingCycle cycle =
            ListingBillingCycle.CreatePaid(
                Guid.NewGuid(),
                1,
                ListingBillingCycleType.InitialActivation,
                PeriodStart,
                PeriodEnd,
                1,
                Guid.NewGuid(),
                PeriodStart);

        Guid refundTransactionId =
            Guid.NewGuid();

        cycle.MarkRefunded(
            refundTransactionId,
            PeriodStart.AddMinutes(5));

        Assert.True(cycle.IsRefunded);

        Assert.Equal(
            refundTransactionId,
            cycle.RefundWalletTransactionId);

        Assert.Throws<DomainException>(() =>
            cycle.MarkRefunded(
                Guid.NewGuid(),
                PeriodStart.AddMinutes(10)));
    }

    [Fact]
    public void FreeCycleCannotBeRefunded()
    {
        ListingBillingCycle cycle =
            ListingBillingCycle.CreateFree(
                Guid.NewGuid(),
                1,
                ListingBillingCycleType.InitialActivation,
                PeriodStart,
                PeriodEnd,
                PeriodStart);

        Assert.Throws<DomainException>(() =>
            cycle.MarkRefunded(
                Guid.NewGuid(),
                PeriodStart.AddMinutes(1)));
    }
}
