using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Moderation;

namespace RentoX.Domain.UnitTests.Listings;

public sealed class BannedWordTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("  Dolandırıcı ", "dolandırıcı")]
    [InlineData("PUL   YUYAN", "pul yuyan")]
    [InlineData("İşıq", "işıq")]
    [InlineData("Dolandır*", "dolandır*")]
    public void AWordIsStoredTrimmedWithSingleSpacesInLowerCase(string input, string expected)
    {
        Assert.Equal(expected, BannedWord.Create(input, Guid.NewGuid(), Now).Word);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("*")]
    [InlineData("a*b")]
    [InlineData("ab**")]
    [InlineData("çox uzun söz çox uzun söz çox uzun söz çox uzun söz çox uzun")]
    public void AnInvalidWordIsRefused(string input)
    {
        Assert.Throws<DomainException>(() => BannedWord.Create(input, Guid.NewGuid(), Now));
    }

    [Fact]
    public void TheAuthorAndAUtcTimeAreRequired()
    {
        Assert.Throws<DomainException>(() => BannedWord.Create("yasaq", Guid.Empty, Now));
        Assert.Throws<DomainException>(() => BannedWord.Create("yasaq", Guid.NewGuid(), Now.ToOffset(TimeSpan.FromHours(4))));
    }

    [Fact]
    public void WholeWordsOnlySoPartOfAnotherWordIsNotAMatch()
    {
        string[] words = ["sik", "spam"];

        Assert.Empty(BannedWordMatcher.Find(words, "Musiqi alətləri icarəyə", "Gözəl səs"));
        Assert.Equal(["spam"], BannedWordMatcher.Find(words, "Bu bir SPAM elanıdır"));
        Assert.Equal(["spam"], BannedWordMatcher.Find(words, "salam", "mətn: spam, təkrar"));
    }

    [Fact]
    public void ATrailingStarMatchesEveryEndingButNotTheMiddleOfAWord()
    {
        string[] words = ["dolandır*"];

        Assert.Single(BannedWordMatcher.Find(words, "Dolandırıcılıq halları"));
        Assert.Single(BannedWordMatcher.Find(words, "dolandırır"));
        Assert.Empty(BannedWordMatcher.Find(words, "yenidolandırma"));
    }

    [Fact]
    public void AzerbaijaniCapitalsMatchTheirSmallLetters()
    {
        string[] words = ["işıq", "ışıq"];

        Assert.Equal(["işıq"], BannedWordMatcher.Find(words, "İŞIQ şirkəti"));
        Assert.Equal(["ışıq"], BannedWordMatcher.Find(words, "IŞIQ şirkəti"));
    }

    [Fact]
    public void APhraseMatchesAcrossAnyWhiteSpace()
    {
        string[] words = ["pul yuyan"];

        Assert.Single(BannedWordMatcher.Find(words, "pul\n  yuyan sxem"));
        Assert.Empty(BannedWordMatcher.Find(words, "pul və yuyan"));
    }

    [Fact]
    public void SpecialCharactersInAWordAreNotTreatedAsPatterns()
    {
        string[] words = ["a.b", "(x)"];

        Assert.Empty(BannedWordMatcher.Find(words, "axb"));
        Assert.Equal(["a.b"], BannedWordMatcher.Find(words, "bu a.b dir"));
        Assert.Equal(["(x)"], BannedWordMatcher.Find(words, "bu (x) dir"));
    }

    [Fact]
    public void NothingToSearchOrNoWordsFindsNothing()
    {
        Assert.Empty(BannedWordMatcher.Find(["spam"], null, "  "));
        Assert.Empty(BannedWordMatcher.Find([], "spam"));
    }
}
