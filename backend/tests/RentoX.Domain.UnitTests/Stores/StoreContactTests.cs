using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Stores;
using Xunit;

namespace RentoX.Domain.UnitTests.Stores;

public sealed class StoreContactTests
{
    private static StoreProfile Active()
    {
        StoreProfile store = StoreProfile.Create(
            Guid.NewGuid(), "Test Store", "test-store", "A store used by the contact tests.",
            "+994501234567", null, null, null, null, null, null);
        store.SetLogo("stores/logo.webp");
        store.SubmitForReview();
        store.Approve();
        return store;
    }

    private static readonly StoreOpenDay[] Weekdays =
        [new(1, 540, 1080), new(2, 540, 1080), new(3, 540, 1080), new(4, 540, 1080), new(5, 540, 1080), new(6, 600, 840)];

    [Theory]
    [InlineData("+994 50 123 45 67", "994501234567")]
    [InlineData("050 123 45 67", "994501234567")]
    [InlineData("501234567", "994501234567")]
    [InlineData("  ", null)]
    [InlineData(null, null)]
    public void AWhatsAppNumberBecomesInternationalDigits(string? input, string? expected)
    {
        Assert.Equal(expected, StoreProfile.NormalizeWhatsApp(input));
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("1234567890123456")]
    public void AWhatsAppNumberOfTheWrongLengthIsRefused(string input)
    {
        Assert.Throws<DomainException>(() => StoreProfile.NormalizeWhatsApp(input));
    }

    [Fact]
    public void ALiveStoreStaysLiveWhenItsContactAndHoursChange()
    {
        StoreProfile store = Active();

        store.UpdateContact(
            "+994 12 345 67 89", "shop@example.com", "Bakı, Nizami küç. 5",
            "https://instagram.com/shop", "https://tiktok.com/@shop", "https://facebook.com/shop", "https://youtube.com/@shop",
            "https://shop.example.com", "050 111 22 33", "Salam! Ünvanı deyin.", Weekdays);

        Assert.Equal(RentoX.Domain.Stores.Enums.StoreStatus.Active, store.Status);
        Assert.Equal("994501112233", store.WhatsAppNumber);
        Assert.Equal("Salam! Ünvanı deyin.", store.WhatsAppMessage);
        Assert.Equal("https://youtube.com/@shop", store.YoutubeUrl);
        Assert.Equal(6, StoreWorkingHours.Read(store.WorkingHours).Count);
    }

    [Fact]
    public void TheWhatsAppMessageIsKeptOnlyWhileThereIsANumber()
    {
        StoreProfile store = Active();

        store.UpdateContact("+994501234567", null, null, null, null, null, null, null, null, "Salam", null);

        Assert.Null(store.WhatsAppNumber);
        Assert.Null(store.WhatsAppMessage);
    }

    [Fact]
    public void AnInvalidContactChangesNothing()
    {
        StoreProfile store = Active();
        store.UpdateContact("+994501234567", null, "Köhnə ünvan", null, null, null, null, null, null, null, null);

        Assert.Throws<DomainException>(() =>
            store.UpdateContact("+994509999999", null, "Yeni ünvan", null, null, null, "not a url", null, null, null, Weekdays));

        Assert.Equal("+994501234567", store.PhoneNumber);
        Assert.Equal("Köhnə ünvan", store.Address);
        Assert.Null(store.WorkingHours);
    }

    [Theory]
    [InlineData(1, 600, 600)]
    [InlineData(1, 900, 600)]
    [InlineData(8, 540, 1080)]
    [InlineData(1, -1, 600)]
    public void InvalidHoursAreRefused(int day, int open, int close)
    {
        Assert.Throws<DomainException>(() => StoreWorkingHours.Serialize([new StoreOpenDay(day, open, close)]));
    }

    [Fact]
    public void ADayCannotBeListedTwiceAndNoHoursMeansNothingIsKept()
    {
        Assert.Throws<DomainException>(() => StoreWorkingHours.Serialize([new(1, 540, 1080), new(1, 600, 700)]));
        Assert.Null(StoreWorkingHours.Serialize([]));
        Assert.Empty(StoreWorkingHours.Read("not json"));
    }

    [Fact]
    public void ASuspendedStoreCannotBeChanged()
    {
        StoreProfile store = Active();
        store.Suspend("Complaints");

        Assert.Throws<DomainException>(() =>
            store.UpdateContact("+994501234567", null, null, null, null, null, null, null, null, null, null));
    }
}
