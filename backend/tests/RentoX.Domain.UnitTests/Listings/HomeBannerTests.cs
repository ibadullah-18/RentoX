using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Content;
using RentoX.Domain.Users.Enums;

namespace RentoX.Domain.UnitTests.Listings;

public sealed class HomeBannerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

    private static HomeBanner Make(
        string? link = null,
        bool active = true,
        DateTimeOffset? starts = null,
        DateTimeOffset? ends = null,
        int order = 1) =>
        HomeBanner.Create(
            "Yay endirimi", "Летняя скидка", "Summer sale",
            "Hər şey 20% ucuz", null, "Everything 20% off",
            link, order, active, starts, ends, Guid.NewGuid(), Now);

    [Fact]
    public void ABannerWithoutAPictureIsNeverLive()
    {
        HomeBanner banner = Make();

        Assert.False(banner.IsLive(Now));

        banner.SetImage("banners/abc.jpg");
        Assert.True(banner.IsLive(Now));
    }

    [Fact]
    public void ASwitchedOffBannerIsNotLiveEvenWithAPicture()
    {
        HomeBanner banner = Make(active: false);
        banner.SetImage("banners/abc.jpg");

        Assert.False(banner.IsLive(Now));
    }

    [Fact]
    public void TheDatesDecideWhenItIsLive()
    {
        HomeBanner banner = Make(starts: Now.AddDays(1), ends: Now.AddDays(5));
        banner.SetImage("banners/abc.jpg");

        Assert.False(banner.IsLive(Now));                  // not yet
        Assert.True(banner.IsLive(Now.AddDays(1)));        // from the start moment
        Assert.True(banner.IsLive(Now.AddDays(3)));
        Assert.False(banner.IsLive(Now.AddDays(5)));       // the end moment is already over
    }

    [Fact]
    public void TheTextComesInTheRequestedLanguageAndAnEmptySubtitleIsNone()
    {
        HomeBanner banner = Make();

        Assert.Equal("Yay endirimi", banner.TitleFor(PreferredLanguage.Azerbaijani));
        Assert.Equal("Летняя скидка", banner.TitleFor(PreferredLanguage.Russian));
        Assert.Equal("Summer sale", banner.TitleFor(PreferredLanguage.English));
        Assert.Null(banner.SubtitleFor(PreferredLanguage.Russian));
        Assert.Equal("Hər şey 20% ucuz", banner.SubtitleFor(PreferredLanguage.Azerbaijani));
    }

    [Theory]
    [InlineData("/destek")]
    [InlineData("/listing/3f9a12cc")]
    [InlineData("https://rentox.az/kampaniya")]
    [InlineData("http://example.com")]
    [InlineData("")]
    [InlineData(null)]
    public void AcceptableLinksAreKept(string? link)
    {
        HomeBanner banner = Make(link);

        Assert.Equal(string.IsNullOrEmpty(link) ? null : link, banner.LinkUrl);
    }

    [Theory]
    [InlineData("//evil.com")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://x.com")]
    [InlineData("destek")]
    public void UnsafeOrUnclearLinksAreRefused(string link)
    {
        Assert.Throws<DomainException>(() => Make(link));
    }

    [Fact]
    public void BadTextOrDatesOrOrderAreRefused()
    {
        Assert.Throws<DomainException>(() => HomeBanner.Create(
            " ", "x", "x", null, null, null, null, 1, true, null, null, Guid.NewGuid(), Now));
        Assert.Throws<DomainException>(() => HomeBanner.Create(
            new string('a', 101), "x", "x", null, null, null, null, 1, true, null, null, Guid.NewGuid(), Now));
        Assert.Throws<DomainException>(() => Make(starts: Now, ends: Now));
        Assert.Throws<DomainException>(() => Make(order: 10001));
        Assert.Throws<DomainException>(() => Make(order: -1));
        Assert.Throws<DomainException>(() => HomeBanner.Create(
            "a", "b", "c", null, null, null, null, 1, true, null, null, Guid.Empty, Now));
    }
}
