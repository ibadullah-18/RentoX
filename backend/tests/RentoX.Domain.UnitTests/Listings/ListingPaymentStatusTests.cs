using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using Xunit;

namespace RentoX.Domain.UnitTests.Listings;

public sealed class ListingPaymentStatusTests
{
    private static Listing CreatePendingListing()
    {
        RentalPeriodUnit periodUnit =
            Enum.GetValues<RentalPeriodUnit>()
                .First();

        Listing listing = Listing.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test listing",
            "Test listing description",
            100,
            "AZN",
            periodUnit);

        listing.AddImage(
            "listings/test.webp",
            "image/webp",
            1000);

        listing.SubmitForReview();

        return listing;
    }

    [Fact]
    public void PendingListingCanRequirePayment()
    {
        Listing listing =
            CreatePendingListing();

        listing.RequirePayment();

        Assert.Equal(
            ListingStatus.PaymentRequired,
            listing.Status);

        Assert.Null(listing.PublishedAtUtc);
        Assert.Null(listing.ExpiresAtUtc);
    }

    [Fact]
    public void PaymentRequiredListingCanBePublished()
    {
        Listing listing =
            CreatePendingListing();

        listing.RequirePayment();

        DateTimeOffset now =
            DateTimeOffset.UtcNow;

        listing.Publish(
            now,
            TimeSpan.FromDays(30));

        Assert.Equal(
            ListingStatus.Active,
            listing.Status);

        Assert.Equal(
            now,
            listing.PublishedAtUtc);

        Assert.Equal(
            now.AddDays(30),
            listing.ExpiresAtUtc);
    }

    [Fact]
    public void DraftListingCannotRequirePayment()
    {
        RentalPeriodUnit periodUnit =
            Enum.GetValues<RentalPeriodUnit>()
                .First();

        Listing listing = Listing.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test listing",
            "Test listing description",
            100,
            "AZN",
            periodUnit);

        Assert.Throws<DomainException>(() =>
            listing.RequirePayment());
    }
}
