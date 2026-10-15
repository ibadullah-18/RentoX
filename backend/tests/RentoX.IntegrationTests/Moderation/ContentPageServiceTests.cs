using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Content;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Content;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Content;
using RentoX.Infrastructure.Persistence;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Moderation;

/// <summary>The fixed pages and the contact details: the texts the site comes with, editing, and what the public sees.</summary>
public sealed class ContentPageServiceTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid Admin = Guid.NewGuid();

    [Fact]
    public async Task ThePagesComeWithTextInAllThreeLanguagesAndTheLanguagesAgreeInStructure()
    {
        await ResetAsync();

        IReadOnlyList<AdminPageItem> pages = await Pages().ListAdminAsync();

        Assert.Equal(ContentPage.AllSlugs, pages.Select(p => p.Slug).ToArray());
        foreach (AdminPageItem page in pages)
        {
            string[] texts = [page.BodyAz, page.BodyRu, page.BodyEn];
            Assert.All(texts, t => Assert.True(t.Length > 100, $"{page.Slug} is too short"));
            Assert.All([page.TitleAz, page.TitleRu, page.TitleEn], t => Assert.False(string.IsNullOrWhiteSpace(t)));

            // The same sections in each language, so no language is missing a part.
            int[] headings = texts.Select(t => t.Split('\n').Count(l => l.StartsWith("## ", StringComparison.Ordinal))).ToArray();
            Assert.True(headings.Distinct().Count() == 1, $"{page.Slug}: the languages have different sections {string.Join("/", headings)}");

            // The same marks in each language, all of them known ones.
            foreach (string text in texts)
            {
                PagePlaceholders.Validate(text);
            }
        }
    }

    [Fact]
    public async Task EveryMarkInTheTextsIsFilledOnceTheDetailsAreKnown()
    {
        await ResetAsync();
        await Info(Admin).UpdateAsync(new SiteInfoValues(
            "Test Sahibkar", "1234567890", "Bakı", "info@rentox.az", "+994 12 345 67 89", "1234", null, null, null, null));

        foreach (string slug in ContentPage.AllSlugs)
        {
            foreach (PreferredLanguage language in Enum.GetValues<PreferredLanguage>())
            {
                PublicPageResult? page = await Pages().GetPublicAsync(slug, language);

                Assert.NotNull(page);
                Assert.DoesNotContain("{{", page.Body);
                if (slug == ContentPage.Terms)
                {
                    Assert.Contains("Test Sahibkar", page.Body);
                    Assert.Contains("+994 12 345 67 89", page.Body);
                }
            }
        }
    }

    [Fact]
    public async Task WithoutTheDetailsTheContactLinesDisappearAndTheRestShowsDashes()
    {
        await ResetAsync();

        PublicPageResult? terms = await Pages().GetPublicAsync(ContentPage.Terms, PreferredLanguage.English);

        Assert.NotNull(terms);
        Assert.DoesNotContain("{{", terms.Body);
        Assert.DoesNotContain("Phone:", terms.Body);
        Assert.DoesNotContain("Hotline:", terms.Body);
        Assert.Contains("tax ID (VÖEN): —", terms.Body);
    }

    [Fact]
    public async Task ThePublicListHasTheTitlesInTheChosenLanguageAndAnUnknownPageIsNotFound()
    {
        await ResetAsync();

        var az = await Pages().ListPublicAsync(PreferredLanguage.Azerbaijani);
        var ru = await Pages().ListPublicAsync(PreferredLanguage.Russian);

        Assert.Equal(6, az.Count);
        Assert.Equal("İstifadə şərtləri", az.Single(p => p.Slug == ContentPage.Terms).Title);
        Assert.Equal("Условия использования", ru.Single(p => p.Slug == ContentPage.Terms).Title);
        Assert.Null(await Pages().GetPublicAsync("not-a-page", PreferredLanguage.Azerbaijani));
    }

    [Fact]
    public async Task AnEditIsKeptWithWhoAndWhenAndTheOthersStayUntouched()
    {
        await ResetAsync();
        var before = (await Pages().ListAdminAsync()).Single(p => p.Slug == ContentPage.About);

        await Pages(Now.AddHours(2)).UpdateAsync(ContentPage.About, new PageInput(
            "Yeni başlıq", "Новый заголовок", "New title",
            "Yeni mətn. {{email}}", "Новый текст. {{email}}", "New text. {{email}}"));

        await using RentoXDbContext db = database.CreateContext();
        ContentPage about = await db.Set<ContentPage>().SingleAsync(p => p.Slug == ContentPage.About);
        Assert.Equal("Yeni başlıq", about.TitleAz);
        Assert.Equal(Admin, about.UpdatedByUserId);
        Assert.Equal(Now.AddHours(2), about.UpdatedAtUtc);
        Assert.True(about.UpdatedAtUtc > before.UpdatedAtUtc);

        ContentPage terms = await db.Set<ContentPage>().SingleAsync(p => p.Slug == ContentPage.Terms);
        Assert.Null(terms.UpdatedByUserId);
    }

    [Fact]
    public async Task ATypoInAMarkAnUnknownPageAndAnonymousEditsAreRefused()
    {
        await ResetAsync();
        var ok = new PageInput("A", "B", "C", "x", "y", "z");

        await Assert.ThrowsAsync<DomainException>(() => Pages().UpdateAsync(
            ContentPage.Terms, ok with { BodyRu = "Опечатка {{emial}}" }));
        await Assert.ThrowsAsync<DomainException>(() => Pages().UpdateAsync("not-a-page", ok));
        await Assert.ThrowsAsync<DomainException>(() => Pages(anonymous: true).UpdateAsync(ContentPage.Terms, ok));

        PublicPageResult? terms = await Pages().GetPublicAsync(ContentPage.Terms, PreferredLanguage.Azerbaijani);
        Assert.NotEqual("A", terms!.Title);
    }

    [Fact]
    public async Task TheContactDetailsAreSavedNormalisedAndReadBack()
    {
        await ResetAsync();
        SiteInfoValues saved = await Info(Admin).UpdateAsync(new SiteInfoValues(
            "  Əli Əliyev ", "123", null, "info@rentox.az", "+994 12 345 67 89", "1234",
            "+994 (50) 123-45-67", "@rentox.az", "rentox", "https://facebook.com/rentox"));

        Assert.Equal("Əli Əliyev", saved.OperatorName);
        Assert.Equal("+994501234567", saved.WhatsApp);
        Assert.Equal("https://www.instagram.com/rentox.az", saved.Instagram);
        Assert.Equal("https://www.tiktok.com/@rentox", saved.TikTok);
        Assert.Equal("https://facebook.com/rentox", saved.Facebook);
        Assert.Equal(saved, await Info(Admin).GetAsync());

        // Clearing a channel removes it, so the footer shows nothing for it.
        SiteInfoValues cleared = await Info(Admin).UpdateAsync(saved with { Instagram = "", TikTok = null });
        Assert.Null(cleared.Instagram);
        Assert.Null(cleared.TikTok);
        Assert.Equal("https://facebook.com/rentox", cleared.Facebook);
    }

    [Fact]
    public async Task BadContactDetailsAreRefusedAndNothingChanges()
    {
        await ResetAsync();
        var empty = new SiteInfoValues(null, null, null, null, null, null, null, null, null, null);

        await Assert.ThrowsAsync<DomainException>(() => Info(Admin).UpdateAsync(empty with { Email = "not-an-email" }));
        await Assert.ThrowsAsync<DomainException>(() => Info(Admin).UpdateAsync(empty with { Instagram = "https://evil.com/x" }));
        await Assert.ThrowsAsync<DomainException>(() => Info(null).UpdateAsync(empty with { Email = "a@b.az" }));

        Assert.Equal(empty, await Info(Admin).GetAsync());
    }

    private async Task ResetAsync()
    {
        await using RentoXDbContext db = database.CreateContext();
        await db.Set<ContentPage>().ExecuteDeleteAsync();
        await db.Set<SiteInfo>().ExecuteDeleteAsync();
    }

    private ContentPageService Pages(DateTimeOffset? at = null, bool anonymous = false)
    {
        RentoXDbContext db = database.CreateContext();
        var user = new TestActor(anonymous ? null : Admin);
        var clock = new TestClock(at ?? Now);
        return new ContentPageService(db, new SiteInfoService(db, clock, user), clock, user);
    }

    private SiteInfoService Info(Guid? actor) =>
        new(database.CreateContext(), new TestClock(Now), new TestActor(actor));

    private sealed class TestClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class TestActor(Guid? userId) : ICurrentUserContext
    {
        public Guid? UserId => userId;

        public bool IsAuthenticated => userId is not null;
    }
}
