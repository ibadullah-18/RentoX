using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Content;
using RentoX.Domain.Users.Enums;

namespace RentoX.Domain.UnitTests.Listings;

public sealed class ContentPageTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    private static ContentPage Make(string slug = ContentPage.Terms) =>
        ContentPage.Create(slug, "Başlıq", "Заголовок", "Title", "Mətn", "Текст", "Text", Now);

    [Fact]
    public void ThereAreSixPagesAndOnlyThoseCanBeCreated()
    {
        Assert.Equal(6, ContentPage.AllSlugs.Count);
        Assert.All(ContentPage.AllSlugs, slug => Assert.Equal(slug, Make(slug).Slug));
        Assert.Throws<DomainException>(() => Make("something-else"));
    }

    [Fact]
    public void TheTextComesInTheRequestedLanguage()
    {
        ContentPage page = Make();

        Assert.Equal("Başlıq", page.TitleFor(PreferredLanguage.Azerbaijani));
        Assert.Equal("Заголовок", page.TitleFor(PreferredLanguage.Russian));
        Assert.Equal("Title", page.TitleFor(PreferredLanguage.English));
        Assert.Equal("Mətn", page.BodyFor(PreferredLanguage.Azerbaijani));
        Assert.Equal("Текст", page.BodyFor(PreferredLanguage.Russian));
        Assert.Equal("Text", page.BodyFor(PreferredLanguage.English));
    }

    [Fact]
    public void UpdatingRecordsWhoAndWhenAndNormalisesLineEndings()
    {
        ContentPage page = Make();
        Guid editor = Guid.NewGuid();

        page.Update("A", "B", "C", "x\r\ny", "z", "w", Now.AddDays(1), editor);

        Assert.Equal(editor, page.UpdatedByUserId);
        Assert.Equal(Now.AddDays(1), page.UpdatedAtUtc);
        Assert.Equal("x\ny", page.BodyAz);
    }

    [Fact]
    public void EveryLanguageNeedsATitleAndATextWithinTheLimits()
    {
        ContentPage page = Make();

        Assert.Throws<DomainException>(() => page.Update(" ", "B", "C", "x", "y", "z", Now, null));
        Assert.Throws<DomainException>(() => page.Update("A", "B", "C", "x", " ", "z", Now, null));
        Assert.Throws<DomainException>(() => page.Update(new string('a', 151), "B", "C", "x", "y", "z", Now, null));
        Assert.Throws<DomainException>(() => page.Update("A", "B", "C", new string('a', 60001), "y", "z", Now, null));
        Assert.Throws<DomainException>(() => page.Update("A", "B", "C", "x", "y", "z", default, null));
        Assert.Throws<DomainException>(() => page.Update("A", "B", "C", "x", "y", "z", Now, Guid.Empty));

        // Nothing changed after the refusals.
        Assert.Equal("Başlıq", page.TitleAz);
    }
}
