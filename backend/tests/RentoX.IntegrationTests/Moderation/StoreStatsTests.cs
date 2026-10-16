using RentoX.Application.Abstractions.Time;
using RentoX.Application.Stores;
using RentoX.Domain.Stores;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Persistence;
using RentoX.Infrastructure.Stores;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Moderation;

/// <summary>What the owner sees: the page counted per day, the owner's own visits left out, contact changes kept.</summary>
public sealed class StoreStatsTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    // 14 October 2026, 12:00 in Baku.
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task EventsAreCountedPerDayVisitorsCountButTheOwnerAndUnknownKindsDoNot()
    {
        (Guid owner, string slug) = await AddActiveStoreAsync();
        await using RentoXDbContext db = database.CreateContext();
        var stats = new StoreStatsService(db, new Clock());

        Assert.True(await stats.RecordAsync(slug, "view", null));
        Assert.True(await stats.RecordAsync(slug, "view", Guid.NewGuid()));
        Assert.True(await stats.RecordAsync(slug, "call", null));
        Assert.True(await stats.RecordAsync(slug, "whatsapp", null));
        Assert.True(await stats.RecordAsync(slug, "message", null));

        // The owner looking at their own store, a kind nobody knows, a store that does not exist.
        Assert.False(await stats.RecordAsync(slug, "view", owner));
        Assert.False(await stats.RecordAsync(slug, "explode", null));
        Assert.False(await stats.RecordAsync("no-such-store", "view", null));

        StoreStatsResult mine = (await stats.GetMineAsync(owner))!;
        Assert.Equal(2, mine.ProfileViews);
        Assert.Equal(1, mine.Calls);
        Assert.Equal(1, mine.WhatsApp);
        Assert.Equal(1, mine.Messages);
        Assert.Equal(2, mine.ProfileViewsLast7Days);
        Assert.Equal(30, mine.Last30Days.Count);
        Assert.Equal(2, mine.Last30Days[^1].Views);
        Assert.Equal(0, mine.Followers);
    }

    [Fact]
    public async Task ManyVisitorsAtTheSameMomentAreAllCounted()
    {
        (Guid owner, string slug) = await AddActiveStoreAsync();

        await Task.WhenAll(Enumerable.Range(0, 20).Select(async _ =>
        {
            await using RentoXDbContext db = database.CreateContext();
            await new StoreStatsService(db, new Clock()).RecordAsync(slug, "view", null);
        }));

        await using RentoXDbContext read = database.CreateContext();
        Assert.Equal(20, (await new StoreStatsService(read, new Clock()).GetMineAsync(owner))!.ProfileViews);
    }

    [Fact]
    public async Task TheOwnerOfALiveStoreChangesContactSocialWhatsAppAndHoursAtAnyTime()
    {
        (Guid owner, _) = await AddActiveStoreAsync();
        await using RentoXDbContext db = database.CreateContext();
        var service = new StoreProfileService(db);

        StoreProfileResult saved = await service.UpdateContactAsync(new UpdateStoreContactCommand(
            owner, "+994 12 345 67 89", "shop@example.com", "Bakı", "https://instagram.com/shop", null, null,
            "https://youtube.com/@shop", null, "050 111 22 33", "Salam!",
            [new StoreHoursDay(1, 540, 1080), new StoreHoursDay(7, 600, 900)]));

        Assert.Equal(3, saved.Status);
        Assert.Equal("994501112233", saved.WhatsAppNumber);
        Assert.Equal("Salam!", saved.WhatsAppMessage);
        Assert.Equal("https://youtube.com/@shop", saved.YoutubeUrl);
        Assert.Equal(2, saved.WorkingHours!.Count);
        Assert.Equal(7, saved.WorkingHours[1].Day);

        StoreProfileResult again = (await service.GetMineAsync(owner))!;
        Assert.Equal(2, again.WorkingHours!.Count);
    }

    [Fact]
    public async Task TheOwnerRewritesTheNameAndDescriptionOfALiveStoreAndItStaysLive()
    {
        (Guid owner, string slug) = await AddActiveStoreAsync();
        await using RentoXDbContext db = database.CreateContext();
        var service = new StoreProfileService(db);

        StoreProfileResult saved = await service.UpdateInfoAsync(owner, "Yeni ad", "Yeni təsvir: ən azı on simvol.");

        Assert.Equal("Yeni ad", saved.Name);
        Assert.Equal(3, saved.Status);
        Assert.Equal(slug, saved.Slug);

        await Assert.ThrowsAsync<RentoX.Domain.Common.Exceptions.DomainException>(() => service.UpdateInfoAsync(owner, "x", "qısa"));
    }

    private async Task<(Guid Owner, string Slug)> AddActiveStoreAsync()
    {
        await using RentoXDbContext db = database.CreateContext();
        Guid id = Guid.NewGuid();
        string userName = $"ss-{id:N}";
        db.Users.Add(new AppUser
        {
            Id = id,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            PhoneNumber = "+99450" + Random.Shared.Next(1000000, 9999999),
            RegisteredAtUtc = Now,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        });
        db.Set<UserProfile>().Add(UserProfile.Create(id, "Sahib", PreferredLanguage.Azerbaijani));
        string slug = "stats-" + id.ToString("N")[..10];
        StoreProfile store = StoreProfile.Create(
            id, "Statistika mağazası", slug, "A store made for the statistics tests.", "+994500000000", null, null, null, null, null, null);
        store.SetLogo("stores/logo.webp");
        store.SubmitForReview();
        store.Approve();
        db.StoreProfiles.Add(store);
        await db.SaveChangesAsync();
        return (id, slug);
    }

    private sealed class Clock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }
}
