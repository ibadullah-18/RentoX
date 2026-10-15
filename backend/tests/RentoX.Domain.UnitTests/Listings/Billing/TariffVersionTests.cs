using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings.Billing;
using Xunit;

namespace RentoX.Domain.UnitTests.Listings.Billing;

public sealed class TariffVersionTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);

    private static TariffVersion Make(
        int free = 5, decimal activation = 1m, decimal bump = 5m, decimal vip = 10m, int days = 7) =>
        TariffVersion.Create(free, activation, bump, vip, days, Guid.NewGuid(), Now);

    [Fact]
    public void AVersionKeepsItsValuesAndWhoChangedThem()
    {
        Guid admin = Guid.NewGuid();

        TariffVersion version = TariffVersion.Create(3, 2.5m, 4m, 12m, 14, admin, Now);

        Assert.Equal(3, version.FreeActiveListingLimit);
        Assert.Equal(2.5m, version.ActivationFee);
        Assert.Equal(4m, version.BumpPrice);
        Assert.Equal(12m, version.VipPrice);
        Assert.Equal(14, version.VipDurationDays);
        Assert.Equal(admin, version.CreatedByUserId);
    }

    [Fact]
    public void FreeServicesAndNoFreeListingsAreAllowed()
    {
        TariffVersion version = Make(free: 0, activation: 0m, bump: 0m, vip: 0m);

        Assert.Equal(0, version.FreeActiveListingLimit);
        Assert.Equal(0m, version.VipPrice);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void TheFreeLimitMustBeInRange(int free)
    {
        Assert.Throws<DomainException>(() => Make(free: free));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1000.01)]
    [InlineData(1.005)]
    public void APriceMustBeInRangeWithTwoDecimals(double price)
    {
        Assert.Throws<DomainException>(() => Make(activation: (decimal)price));
        Assert.Throws<DomainException>(() => Make(bump: (decimal)price));
        Assert.Throws<DomainException>(() => Make(vip: (decimal)price));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(91)]
    public void TheVipLengthMustBeInRange(int days)
    {
        Assert.Throws<DomainException>(() => Make(days: days));
    }

    [Fact]
    public void WhoChangedItAndWhenAreRequired()
    {
        Assert.Throws<DomainException>(() =>
            TariffVersion.Create(5, 1m, 5m, 10m, 7, Guid.Empty, Now));
        Assert.Throws<DomainException>(() =>
            TariffVersion.Create(5, 1m, 5m, 10m, 7, Guid.NewGuid(), default));
    }
}
