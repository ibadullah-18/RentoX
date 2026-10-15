using System.Reflection;
using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Content;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Content;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Content;

public sealed class ContentPageService(
    RentoXDbContext dbContext,
    ISiteInfoService siteInfoService,
    IClock clock,
    ICurrentUserContext currentUserContext)
    : IContentPageService
{
    /// <summary>The titles the pages start with (the text itself is in the embedded .md files).</summary>
    private static readonly Dictionary<string, (string Az, string Ru, string En)> StartingTitles = new()
    {
        [ContentPage.Terms] = ("İstifadə şərtləri", "Условия использования", "Terms of Use"),
        [ContentPage.Privacy] = ("Məxfilik siyasəti", "Политика конфиденциальности", "Privacy Policy"),
        [ContentPage.About] = ("Haqqımızda", "О нас", "About us"),
        [ContentPage.Cooperation] = ("Əməkdaşlıq", "Сотрудничество", "Cooperation"),
        [ContentPage.Rules] = ("Qaydalar və təhlükəsizlik", "Правила и безопасность", "Rules and safety"),
        [ContentPage.Faq] = ("Tez-tez verilən suallar", "Частые вопросы", "Frequently asked questions")
    };

    public async Task<IReadOnlyList<PublicPageSummary>> ListPublicAsync(
        PreferredLanguage language, CancellationToken cancellationToken = default)
    {
        await EnsureStartingPagesAsync(cancellationToken);

        List<ContentPage> pages = await dbContext.Set<ContentPage>().AsNoTracking().ToListAsync(cancellationToken);
        return ContentPage.AllSlugs
            .Select(slug => pages.SingleOrDefault(p => p.Slug == slug))
            .OfType<ContentPage>()
            .Select(p => new PublicPageSummary(p.Slug, p.TitleFor(language)))
            .ToArray();
    }

    public async Task<PublicPageResult?> GetPublicAsync(
        string slug, PreferredLanguage language, CancellationToken cancellationToken = default)
    {
        if (!ContentPage.AllSlugs.Contains(slug))
        {
            return null;
        }

        await EnsureStartingPagesAsync(cancellationToken);

        ContentPage? page = await dbContext.Set<ContentPage>().AsNoTracking()
            .SingleOrDefaultAsync(p => p.Slug == slug, cancellationToken);
        if (page is null)
        {
            return null;
        }

        SiteInfoValues info = await siteInfoService.GetAsync(cancellationToken);
        return new PublicPageResult(
            page.Slug,
            page.TitleFor(language),
            PagePlaceholders.Resolve(page.BodyFor(language), info),
            page.UpdatedAtUtc);
    }

    public async Task<IReadOnlyList<AdminPageItem>> ListAdminAsync(CancellationToken cancellationToken = default)
    {
        await EnsureStartingPagesAsync(cancellationToken);

        List<ContentPage> pages = await dbContext.Set<ContentPage>().AsNoTracking().ToListAsync(cancellationToken);
        return ContentPage.AllSlugs
            .Select(slug => pages.SingleOrDefault(p => p.Slug == slug))
            .OfType<ContentPage>()
            .Select(p => new AdminPageItem(
                p.Slug, p.TitleAz, p.TitleRu, p.TitleEn, p.BodyAz, p.BodyRu, p.BodyEn, p.UpdatedAtUtc))
            .ToArray();
    }

    public async Task UpdateAsync(string slug, PageInput input, CancellationToken cancellationToken = default)
    {
        Guid? actor = currentUserContext.UserId;
        if (!currentUserContext.IsAuthenticated || !actor.HasValue || actor.Value == Guid.Empty)
        {
            throw new DomainException("An authenticated admin is required.");
        }

        if (!ContentPage.AllSlugs.Contains(slug))
        {
            throw new DomainException("Page is unknown.");
        }

        // A mistyped mark would show on the live page: refuse it here.
        PagePlaceholders.Validate(input.BodyAz);
        PagePlaceholders.Validate(input.BodyRu);
        PagePlaceholders.Validate(input.BodyEn);

        await EnsureStartingPagesAsync(cancellationToken);

        ContentPage page = await dbContext.Set<ContentPage>().SingleAsync(p => p.Slug == slug, cancellationToken);
        page.Update(
            input.TitleAz, input.TitleRu, input.TitleEn,
            input.BodyAz, input.BodyRu, input.BodyEn,
            clock.UtcNow, actor.Value);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>The first time the pages are needed, the texts the site comes with are put in the database.</summary>
    private async Task EnsureStartingPagesAsync(CancellationToken cancellationToken)
    {
        HashSet<string> have = (await dbContext.Set<ContentPage>().AsNoTracking()
            .Select(p => p.Slug)
            .ToListAsync(cancellationToken)).ToHashSet();
        if (ContentPage.AllSlugs.All(have.Contains))
        {
            return;
        }

        foreach (string slug in ContentPage.AllSlugs.Where(s => !have.Contains(s)))
        {
            (string az, string ru, string en) = StartingTitles[slug];
            dbContext.Set<ContentPage>().Add(ContentPage.Create(
                slug, az, ru, en,
                StartingText(slug, "az"), StartingText(slug, "ru"), StartingText(slug, "en"),
                clock.UtcNow));
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Another request put them in at the same moment: that is fine, read what is there.
            dbContext.ChangeTracker.Clear();
        }
    }

    internal static string StartingText(string slug, string language)
    {
        string name = $"RentoX.Infrastructure.Content.Defaults.{slug}.{language}.md";
        using Stream stream = typeof(ContentPageService).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"The starting text {name} is missing from the build.");
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}
