using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Content;
using RentoX.Application.Files;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Content;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Content;
using RentoX.Infrastructure.Persistence;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Content;

/// <summary>
/// A campaign can have its own page (rentox.az/birbank): the banner leads to it, the page is shown while the campaign
/// is on, says so when it is over, and never takes a name the site already uses.
/// </summary>
public sealed class CampaignPageTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 8, 0, 0, TimeSpan.Zero);

    private static BannerInput Input(string slug, int platform = 3, DateTimeOffset? starts = null, DateTimeOffset? ends = null) => new(
        "Birbank kampaniyası", "Кампания Birbank", "Birbank campaign",
        "Alt yazı", null, null,
        null, 1, true, starts, ends,
        PageSlug: slug,
        PageBodyAz: "Birinci abzas.\n\nİkinci abzas.",
        PageBodyRu: "Текст", PageBodyEn: "Text",
        PageButtonLabelAz: "Bax", PageButtonLabelRu: "Смотреть", PageButtonLabelEn: "See",
        PageButtonUrl: "/search",
        Platform: platform);

    [Fact]
    public async Task ACampaignWithAPageLeadsToItAndThePageIsShownInTheLanguageOfTheVisitor()
    {
        string slug = "birbank-" + Guid.NewGuid().ToString("N")[..8];
        await using RentoXDbContext db = database.CreateContext();
        HomeBannerService service = Service(db);

        Guid id = await service.CreateAsync(Input(slug));
        await GiveImageAsync(id);

        // The banner link is the page, whatever was written as the link.
        AdminBannerItem item = (await service.ListAsync()).Single(b => b.Id == id);
        Assert.Equal("/" + slug, item.LinkUrl);
        Assert.Equal(slug, item.PageSlug);

        PublicBannerItem live = (await service.GetLiveAsync(PreferredLanguage.Russian)).Single(b => b.Id == id);
        Assert.Equal("/" + slug, live.LinkUrl);
        Assert.Equal(slug, live.PageSlug);

        CampaignPageItem page = (await service.GetPageAsync(slug.ToUpperInvariant(), PreferredLanguage.Russian))!;
        Assert.Equal("Кампания Birbank", page.Title);
        Assert.Equal("Текст", page.Body);
        Assert.Equal("Смотреть", page.ButtonLabel);
        Assert.Equal("/search", page.ButtonUrl);
        Assert.False(page.IsOver);

        Assert.Null(await service.GetPageAsync("no-such-" + Guid.NewGuid().ToString("N"), PreferredLanguage.Azerbaijani));
    }

    [Fact]
    public async Task ANameTheSiteUsesOrAnotherCampaignHasIsRefusedAndABadNameToo()
    {
        string slug = "dup-" + Guid.NewGuid().ToString("N")[..8];
        await using RentoXDbContext db = database.CreateContext();
        HomeBannerService service = Service(db);

        await service.CreateAsync(Input(slug));
        await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(Input(slug)));
        await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(Input("destek")));
        await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(Input("admin")));
        await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(Input("a")));
        await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(Input("bad name")));
        await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(Input("bad--dash")));
    }

    [Fact]
    public async Task APageIsHiddenBeforeItStartsOrWhenSwitchedOffAndSaysSoWhenTheCampaignIsOver()
    {
        string soon = "soon-" + Guid.NewGuid().ToString("N")[..8];
        string over = "over-" + Guid.NewGuid().ToString("N")[..8];
        await using RentoXDbContext db = database.CreateContext();
        HomeBannerService service = Service(db);

        Guid soonId = await service.CreateAsync(Input(soon, starts: Now.AddDays(2)));
        Guid overId = await service.CreateAsync(Input(over, starts: Now.AddDays(-9), ends: Now.AddDays(-1)));
        await GiveImageAsync(soonId);
        await GiveImageAsync(overId);

        Assert.Null(await service.GetPageAsync(soon, PreferredLanguage.Azerbaijani));
        CampaignPageItem ended = (await service.GetPageAsync(over, PreferredLanguage.Azerbaijani))!;
        Assert.True(ended.IsOver);

        // Neither is on the home page.
        IReadOnlyList<PublicBannerItem> live = await service.GetLiveAsync(PreferredLanguage.Azerbaijani);
        Assert.DoesNotContain(live, b => b.Id == soonId || b.Id == overId);
    }

    [Fact]
    public async Task ABannerMadeForTheWebsiteIsNotShownInTheAppsAndBack()
    {
        string slug = "web-" + Guid.NewGuid().ToString("N")[..8];
        await using RentoXDbContext db = database.CreateContext();
        HomeBannerService service = Service(db);

        Guid web = await service.CreateAsync(Input(slug, platform: 1));
        await GiveImageAsync(web);

        Assert.Contains(await service.GetLiveAsync(PreferredLanguage.Azerbaijani, 1), b => b.Id == web);
        Assert.DoesNotContain(await service.GetLiveAsync(PreferredLanguage.Azerbaijani, 2), b => b.Id == web);
        Assert.Contains(await service.GetLiveAsync(PreferredLanguage.Azerbaijani, 3), b => b.Id == web);
    }

    [Fact]
    public async Task ABannerWithoutAPageKeepsItsOwnLinkAndNoLinkMeansNotClickable()
    {
        await using RentoXDbContext db = database.CreateContext();
        HomeBannerService service = Service(db);

        Guid linked = await service.CreateAsync(Input("") with { LinkUrl = "/destek", PageSlug = null });
        Guid plain = await service.CreateAsync(Input("") with { LinkUrl = null, PageSlug = null });

        IReadOnlyList<AdminBannerItem> all = await service.ListAsync();
        Assert.Equal("/destek", all.Single(b => b.Id == linked).LinkUrl);
        Assert.Null(all.Single(b => b.Id == plain).LinkUrl);
    }

    private async Task GiveImageAsync(Guid id)
    {
        await using RentoXDbContext db = database.CreateContext();
        HomeBanner banner = await db.Set<HomeBanner>().SingleAsync(b => b.Id == id);
        banner.SetImage("banners/test.jpg");
        await db.SaveChangesAsync();
    }

    private static HomeBannerService Service(RentoXDbContext db) =>
        new(db, new NoStorage(), new Clock(), new Actor(Guid.NewGuid()));

    private sealed class Clock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class Actor(Guid userId) : ICurrentUserContext
    {
        public Guid? UserId => userId;

        public bool IsAuthenticated => true;
    }

    private sealed class NoStorage : IFileStorage
    {
        public Task<StoredFileResult> SaveAsync(
            FileStorageArea area, Stream content, string contentType, string extension, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream?>(null);

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
