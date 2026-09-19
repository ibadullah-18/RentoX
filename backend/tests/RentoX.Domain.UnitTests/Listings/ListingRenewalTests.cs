using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using Xunit;

namespace RentoX.Domain.UnitTests.Listings;

public sealed class ListingRenewalTests
{
    private static readonly TimeSpan Lifetime =
        TimeSpan.FromDays(30);

    private static readonly DateTimeOffset PublishedAt =
        new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CannotRenewBeforeExpiration()
    {
        Listing listing = CreateActiveListing();

        Assert.Throws<DomainException>(() =>
            listing.Renew(
                PublishedAt.AddDays(29),
                Lifetime));

        Assert.Equal(
            PublishedAt.AddDays(30),
            listing.ExpiresAtUtc!.Value);
    }

    [Fact]
    public void CanRenewAtExpiration()
    {
        Listing listing = CreateActiveListing();
        DateTimeOffset renewedAt =
            PublishedAt.AddDays(30);

        listing.Renew(renewedAt, Lifetime);

        Assert.Equal(
            ListingStatus.Active,
            listing.Status);

        Assert.Equal(
            renewedAt,
            listing.PublishedAtUtc!.Value);

        Assert.Equal(
            renewedAt.AddDays(30),
            listing.ExpiresAtUtc!.Value);
    }

    [Fact]
    public void CannotRenewAgainDuringNewPeriod()
    {
        Listing listing = CreateActiveListing();
        DateTimeOffset renewedAt =
            PublishedAt.AddDays(30);

        listing.Renew(renewedAt, Lifetime);

        Assert.Throws<DomainException>(() =>
            listing.Renew(
                renewedAt.AddSeconds(1),
                Lifetime));

        Assert.Equal(
            renewedAt.AddDays(30),
            listing.ExpiresAtUtc!.Value);
    }

    [Fact]
    public void CanRenewDeactivatedListingAfterExpiration()
    {
        Listing listing = CreateActiveListing();
        listing.Deactivate();

        DateTimeOffset renewedAt =
            PublishedAt.AddDays(31);

        listing.Renew(renewedAt, Lifetime);

        Assert.Equal(
            ListingStatus.Active,
            listing.Status);

        Assert.Equal(
            renewedAt.AddDays(30),
            listing.ExpiresAtUtc!.Value);
    }

    private static Listing CreateActiveListing()
    {
        Listing listing = Listing.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test rental listing",
            "Listing renewal test description",
            100m,
            "AZN",
            Enum.GetValues<RentalPeriodUnit>()[0]);

        listing.AddImage(
            "tests/listing.jpg",
            "image/jpeg",
            100);

        listing.SubmitForReview();
        listing.Publish(PublishedAt, Lifetime);

        return listing;
    }
}
