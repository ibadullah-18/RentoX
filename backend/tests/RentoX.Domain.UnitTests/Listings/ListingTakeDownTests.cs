using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using Xunit;

namespace RentoX.Domain.UnitTests.Listings;

public sealed class ListingTakeDownTests
{
    private static readonly DateTimeOffset PublishedAt =
        new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

    private static Listing CreatePending()
    {
        Listing listing = Listing.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test rental listing",
            "Listing take down test description",
            100m,
            "AZN",
            RentalPeriodUnit.Day);

        listing.AddImage("tests/listing.jpg", "image/jpeg", 100);
        listing.SubmitForReview();
        return listing;
    }

    private static Listing CreateActive()
    {
        Listing listing = CreatePending();
        listing.Publish(PublishedAt, TimeSpan.FromDays(30));
        return listing;
    }

    [Fact]
    public void ALiveListingIsRejectedWithTheReason()
    {
        Listing listing = CreateActive();

        listing.TakeDown("  Spam  ");

        Assert.Equal(ListingStatus.Rejected, listing.Status);
        Assert.Equal("Spam", listing.RejectionReason);
        Assert.Null(listing.PublishedAtUtc);
        Assert.Null(listing.ExpiresAtUtc);
    }

    [Fact]
    public void APausedAndAPendingListingCanBeTakenDown()
    {
        Listing paused = CreateActive();
        paused.Deactivate();
        paused.TakeDown("Complaint");
        Assert.Equal(ListingStatus.Rejected, paused.Status);

        Listing pending = CreatePending();
        pending.TakeDown("Complaint");
        Assert.Equal(ListingStatus.Rejected, pending.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AReasonIsRequired(string reason)
    {
        Listing listing = CreateActive();

        Assert.Throws<DomainException>(() => listing.TakeDown(reason));
        Assert.Equal(ListingStatus.Active, listing.Status);
    }

    [Fact]
    public void AReasonLongerThanAThousandCharactersIsRefused()
    {
        Listing listing = CreateActive();

        Assert.Throws<DomainException>(() => listing.TakeDown(new string('x', 1001)));
    }

    [Fact]
    public void ADeletedOrAlreadyRejectedListingCannotBeTakenDown()
    {
        Listing deleted = CreateActive();
        deleted.MarkDeleted(PublishedAt.AddDays(1));
        Assert.Throws<DomainException>(() => deleted.TakeDown("Spam"));

        Listing rejected = CreatePending();
        rejected.Reject("Spam");
        Assert.Throws<DomainException>(() => rejected.TakeDown("Spam"));
    }
}
