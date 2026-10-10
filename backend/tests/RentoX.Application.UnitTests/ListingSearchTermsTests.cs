using RentoX.Application.Listings.Search;

namespace RentoX.Application.UnitTests;

public sealed class ListingSearchTermsTests
{
    [Theory]
    [InlineData("Maşın", "masin")]
    [InlineData("MAŞIN", "masin")]
    [InlineData("masin", "masin")]
    [InlineData("Əfsanə İSTANBUL", "efsane istanbul")]
    [InlineData("Çörək, Şöbə-ÜĞ!", "corek sobe ug")]
    [InlineData("  Nizami   küçəsində  ", "nizami kucesinde")]
    [InlineData("Мотоцикл Ёлка", "мотоцикл елка")]
    [InlineData("3-otaqlı", "3 otaqli")]
    [InlineData("", "")]
    [InlineData("!!! ???", "")]
    public void NormalizeFoldsAzerbaijaniLettersAndPunctuation(
        string input,
        string expected)
    {
        Assert.Equal(expected, ListingSearchTerms.Normalize(input));
    }

    [Fact]
    public void NormalizeHandlesNull()
    {
        Assert.Equal(string.Empty, ListingSearchTerms.Normalize(null));
    }

    [Fact]
    public void ParseSplitsWordsAndKeepsTheTypedWordFirst()
    {
        IReadOnlyList<SearchToken> tokens =
            ListingSearchTerms.Parse("Toyta MAŞIN");

        Assert.Equal(["toyta", "masin"], tokens.Select(t => t.Text));
        Assert.Equal("toyta", tokens[0].Alternatives[0]);
        Assert.Equal("masin", tokens[1].Alternatives[0]);
    }

    [Theory]
    [InlineData("maşın", "avtomobil")]
    [InlineData("avtomobil", "masin")]
    [InlineData("car", "masin")]
    [InlineData("mənzil", "kvartira")]
    [InlineData("квартира", "menzil")]
    [InlineData("мотоцикл", "motosiklet")]
    public void SynonymsAreFoundInEveryLanguage(string typed, string expected)
    {
        SearchToken token = Assert.Single(ListingSearchTerms.Parse(typed));

        Assert.Contains(expected, token.Alternatives);
    }

    [Fact]
    public void PluralFormsFallBackToTheSingularAndItsSynonyms()
    {
        SearchToken token =
            Assert.Single(ListingSearchTerms.Parse("maşınlar"));

        Assert.Equal("masinlar", token.Alternatives[0]);
        Assert.Contains("masin", token.Alternatives);
        Assert.Contains("avtomobil", token.Alternatives);
    }

    [Fact]
    public void ShortWordsAreNotStemmed()
    {
        // "ler" must not be cut off a short word like "ler" or "gler".
        SearchToken token = Assert.Single(ListingSearchTerms.Parse("ler"));

        Assert.Equal(["ler"], token.Alternatives);
    }

    [Fact]
    public void StopWordsAndDuplicatesAreDropped()
    {
        IReadOnlyList<SearchToken> tokens =
            ListingSearchTerms.Parse("ev və ev üçün villa");

        Assert.Equal(["ev", "villa"], tokens.Select(t => t.Text));
    }

    [Fact]
    public void AtMostSixWordsAreUsed()
    {
        IReadOnlyList<SearchToken> tokens =
            ListingSearchTerms.Parse("a1 b2 c3 d4 e5 f6 g7 h8");

        Assert.Equal(ListingSearchTerms.MaximumTokens, tokens.Count);
    }

    [Fact]
    public void AlternativesAreCappedAndUnique()
    {
        foreach (string word in new[] { "masin", "menzil", "kiraye", "ev" })
        {
            SearchToken token =
                Assert.Single(ListingSearchTerms.Parse(word));

            Assert.True(
                token.Alternatives.Count <=
                ListingSearchTerms.MaximumAlternatives);
            Assert.Equal(
                token.Alternatives.Count,
                token.Alternatives.Distinct().Count());
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!!")]
    public void NothingSearchableGivesNoWords(string? input)
    {
        Assert.Empty(ListingSearchTerms.Parse(input));
    }
}
