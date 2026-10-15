using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Content;

namespace RentoX.Domain.UnitTests.Listings;

public sealed class SiteInfoTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    private static SiteInfo With(
        string? email = null, string? phone = null, string? hotline = null, string? whatsApp = null,
        string? instagram = null, string? tikTok = null, string? facebook = null)
    {
        SiteInfo info = SiteInfo.CreateEmpty();
        info.Update(null, null, null, email, phone, hotline, whatsApp, instagram, tikTok, facebook, Now, Guid.NewGuid());
        return info;
    }

    [Fact]
    public void EmptyDetailsStayEmptySoNothingIsShown()
    {
        SiteInfo info = With(email: "  ", phone: "", instagram: null);

        Assert.Null(info.Email);
        Assert.Null(info.Phone);
        Assert.Null(info.Instagram);
        Assert.Null(info.WhatsApp);
    }

    [Theory]
    [InlineData("info@rentox.az")]
    [InlineData("  support@rentox.az ")]
    public void AGoodEmailIsKeptTrimmed(string value)
    {
        Assert.Equal(value.Trim(), With(email: value).Email);
    }

    [Theory]
    [InlineData("no-at-sign")]
    [InlineData("a@b")]
    [InlineData("two words@x.az")]
    public void ABadEmailIsRefused(string value)
    {
        Assert.Throws<DomainException>(() => With(email: value));
    }

    [Theory]
    [InlineData("+994 12 345 67 89")]
    [InlineData("1234")]
    [InlineData("(012) 345-67-89")]
    public void NormalPhoneNumbersAreAccepted(string value)
    {
        Assert.Equal(value, With(phone: value, hotline: value).Phone);
    }

    [Theory]
    [InlineData("call me")]
    [InlineData("12")]
    [InlineData("+994<script>")]
    public void LettersAndTooShortNumbersAreRefused(string value)
    {
        Assert.Throws<DomainException>(() => With(phone: value));
        Assert.Throws<DomainException>(() => With(hotline: value));
    }

    [Fact]
    public void AWhatsAppNumberIsStoredAsPlusAndDigits()
    {
        Assert.Equal("+994501234567", With(whatsApp: "+994 (50) 123-45-67").WhatsApp);
        Assert.Equal("+994501234567", With(whatsApp: "994501234567").WhatsApp);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("+994abc501234567")]
    [InlineData("https://wa.me/994501234567")]
    public void ABadWhatsAppNumberIsRefused(string value)
    {
        Assert.Throws<DomainException>(() => With(whatsApp: value));
    }

    [Fact]
    public void ASocialProfileNameBecomesTheNetworksAddress()
    {
        SiteInfo info = With(instagram: "@rentox.az", tikTok: "rentox", facebook: "rentox.az");

        Assert.Equal("https://www.instagram.com/rentox.az", info.Instagram);
        Assert.Equal("https://www.tiktok.com/@rentox", info.TikTok);
        Assert.Equal("https://www.facebook.com/rentox.az", info.Facebook);
    }

    [Fact]
    public void AFullAddressOnTheNetworksOwnSiteIsKeptAndMadeSecure()
    {
        SiteInfo info = With(
            instagram: "http://instagram.com/rentox",
            tikTok: "https://www.tiktok.com/@rentox?lang=az",
            facebook: "https://m.facebook.com/rentox");

        Assert.Equal("https://instagram.com/rentox", info.Instagram);
        Assert.Equal("https://www.tiktok.com/@rentox?lang=az", info.TikTok);
        Assert.Equal("https://m.facebook.com/rentox", info.Facebook);
    }

    [Theory]
    [InlineData("https://evil.com/instagram.com")]
    [InlineData("https://instagram.com.evil.com/x")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://instagram.com/x")]
    [InlineData("https://user:pass@instagram.com/x")]
    [InlineData("a")]
    [InlineData("two words")]
    public void AnythingThatIsNotThatNetworkIsRefused(string value)
    {
        Assert.Throws<DomainException>(() => With(instagram: value));
    }

    [Fact]
    public void AnotherNetworksAddressIsRefused()
    {
        Assert.Throws<DomainException>(() => With(facebook: "https://instagram.com/rentox"));
        Assert.Throws<DomainException>(() => With(tikTok: "https://facebook.com/rentox"));
    }

    [Fact]
    public void TooLongTextAndABadTimeOrEditorAreRefused()
    {
        SiteInfo info = SiteInfo.CreateEmpty();

        Assert.Throws<DomainException>(() => info.Update(
            new string('a', 151), null, null, null, null, null, null, null, null, null, Now, Guid.NewGuid()));
        Assert.Throws<DomainException>(() => info.Update(
            null, null, null, null, null, null, null, null, null, null, default, Guid.NewGuid()));
        Assert.Throws<DomainException>(() => info.Update(
            null, null, null, null, null, null, null, null, null, null, Now, Guid.Empty));
    }
}
