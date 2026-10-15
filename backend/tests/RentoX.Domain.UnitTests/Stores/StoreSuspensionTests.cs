using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Stores;
using RentoX.Domain.Stores.Enums;
using Xunit;

namespace RentoX.Domain.UnitTests.Stores;

public sealed class StoreSuspensionTests
{
    private static StoreProfile Make(string name, string slug) =>
        StoreProfile.Create(
            Guid.NewGuid(), name, slug, "A store used by the suspension tests.",
            "+994501234567", null, null, null, null, null, null);

    private static StoreProfile CreateActive()
    {
        StoreProfile store = Make("Test Store", "test-store");

        store.SetLogo("stores/logo.webp");
        store.SubmitForReview();
        store.Approve();
        return store;
    }

    [Fact]
    public void AnActiveStoreIsSuspendedWithTheReason()
    {
        StoreProfile store = CreateActive();

        store.Suspend("  Complaints  ");

        Assert.Equal(StoreStatus.Suspended, store.Status);
        Assert.Equal("Complaints", store.RejectionReason);
    }

    [Fact]
    public void ASuspendedStoreCanBeReinstated()
    {
        StoreProfile store = CreateActive();
        store.Suspend("Complaints");

        store.Reinstate();

        Assert.Equal(StoreStatus.Active, store.Status);
        Assert.Null(store.RejectionReason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AReasonIsRequired(string reason)
    {
        StoreProfile store = CreateActive();

        Assert.Throws<DomainException>(() => store.Suspend(reason));
        Assert.Equal(StoreStatus.Active, store.Status);
    }

    [Fact]
    public void OnlyActiveStoresAreSuspendedAndOnlySuspendedOnesReinstated()
    {
        StoreProfile draft = Make("Draft Store", "draft-store");
        Assert.Throws<DomainException>(() => draft.Suspend("Spam"));
        Assert.Throws<DomainException>(() => draft.Reinstate());

        StoreProfile active = CreateActive();
        Assert.Throws<DomainException>(() => active.Reinstate());
    }
}
