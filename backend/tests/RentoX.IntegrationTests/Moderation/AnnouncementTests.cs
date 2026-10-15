using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Admin;
using RentoX.Infrastructure.Accounts;
using RentoX.Domain.Auditing;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Stores;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Notifications;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Admin;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Persistence;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Moderation;

/// <summary>Messages from the staff: to one person or to everyone with an active account.</summary>
public sealed class AnnouncementTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AMessageToOnePersonReachesOnlyThem()
    {
        Guid actor = await AddUserAsync();
        Guid target = await AddUserAsync();
        Guid other = await AddUserAsync();

        var result = await Service(actor).SendAsync(target, AnnouncementAudience.Everyone, "Salam", "Yeni xidmət əlavə olundu.", null);

        Assert.Equal(1, result.Recipients);
        await using RentoXDbContext db = database.CreateContext();
        Notification got = await db.Set<Notification>().SingleAsync(n => n.UserId == target);
        Assert.Equal(NotificationType.Announcement, got.Type);
        Assert.Equal("Salam", got.Title);
        Assert.False(await db.Set<Notification>().AnyAsync(n => n.UserId == other));

        AuditLogEntry entry = await db.Set<AuditLogEntry>()
            .SingleAsync(a => a.Action == AuditAction.AnnouncementSent && a.TargetId == target);
        Assert.Equal(actor, entry.ActorUserId);
        Assert.Equal(1, entry.CurrentValue);
    }

    [Fact]
    public async Task AMessageToEveryoneSkipsBlockedAccounts()
    {
        Guid actor = await AddUserAsync();
        Guid active = await AddUserAsync();
        Guid blocked = await AddUserAsync(block: true);

        var result = await Service(actor).SendAsync(null, AnnouncementAudience.Everyone, "Hamıya", "Texniki işlər gedir.", "/destek");

        Assert.True(result.Recipients >= 2);
        await using RentoXDbContext db = database.CreateContext();
        Assert.True(await db.Set<Notification>().AnyAsync(n => n.UserId == active && n.Title == "Hamıya"));
        Assert.True(await db.Set<Notification>().AnyAsync(n => n.UserId == actor && n.Title == "Hamıya"));
        Assert.False(await db.Set<Notification>().AnyAsync(n => n.UserId == blocked));
        Assert.True(await db.Set<AuditLogEntry>().AnyAsync(a =>
            a.Action == AuditAction.AnnouncementSent && a.ActorUserId == actor && a.CurrentValue == result.Recipients));
    }

    [Fact]
    public async Task APersonWhoStoppedGroupMessagesStillGetsADirectOne()
    {
        Guid actor = await AddUserAsync();
        Guid quiet = await AddUserAsync(announcements: false);

        await Service(actor).SendAsync(null, AnnouncementAudience.Everyone, "Qrup", "Hamıya.", null);
        await using (RentoXDbContext db = database.CreateContext())
        {
            Assert.False(await db.Set<Notification>().AnyAsync(n => n.UserId == quiet));
        }

        await Service(actor).SendAsync(quiet, AnnouncementAudience.Everyone, "Şəxsi", "Yalnız sizə.", null);
        await using (RentoXDbContext db = database.CreateContext())
        {
            Assert.True(await db.Set<Notification>().AnyAsync(n => n.UserId == quiet && n.Title == "Şəxsi"));
        }
    }

    [Fact]
    public async Task ThePersonCanTurnGroupMessagesOffAndOn()
    {
        Guid person = await AddUserAsync();
        await using RentoXDbContext db = database.CreateContext();
        var settings = new AccountNotificationSettingsService(db);

        Assert.True(await settings.GetAnnouncementsEnabledAsync(person));
        Assert.True(await settings.SetAnnouncementsEnabledAsync(person, false));
        Assert.False(await settings.GetAnnouncementsEnabledAsync(person));
        Assert.True(await settings.SetAnnouncementsEnabledAsync(person, true));
        Assert.True(await settings.GetAnnouncementsEnabledAsync(person));

        Assert.Null(await settings.GetAnnouncementsEnabledAsync(Guid.NewGuid()));
        Assert.False(await settings.SetAnnouncementsEnabledAsync(Guid.NewGuid(), false));
    }

    [Fact]
    public async Task ABlockedOrUnknownPersonCannotBeWrittenTo()
    {
        Guid actor = await AddUserAsync();
        Guid blocked = await AddUserAsync(block: true);

        await Assert.ThrowsAsync<DomainException>(() => Service(actor).SendAsync(blocked, AnnouncementAudience.Everyone, "A", "B", null));
        await Assert.ThrowsAsync<DomainException>(() => Service(actor).SendAsync(Guid.NewGuid(), AnnouncementAudience.Everyone, "A", "B", null));
    }

    [Fact]
    public async Task AnEmptyTitleOrTextWritesNothing()
    {
        Guid actor = await AddUserAsync();
        Guid target = await AddUserAsync();

        await Assert.ThrowsAsync<DomainException>(() => Service(actor).SendAsync(target, AnnouncementAudience.Everyone, " ", "Mətn", null));
        await Assert.ThrowsAsync<DomainException>(() => Service(actor).SendAsync(target, AnnouncementAudience.Everyone, "Başlıq", "", null));

        await using RentoXDbContext db = database.CreateContext();
        Assert.False(await db.Set<Notification>().AnyAsync(n => n.UserId == target));
        Assert.False(await db.Set<AuditLogEntry>().AnyAsync(a => a.TargetId == target));
    }

    [Fact]
    public async Task TheHistoryCountsWhoGotAMessageAndWhoReadIt()
    {
        Guid actor = await AddUserAsync();
        Guid first = await AddUserAsync();
        string title = "Tarix " + Guid.NewGuid().ToString("N")[..8];

        await Service(actor).SendAsync(first, AnnouncementAudience.Everyone, title, "Mətn.", null);

        await using (RentoXDbContext db = database.CreateContext())
        {
            Notification n = await db.Set<Notification>().SingleAsync(x => x.UserId == first && x.Title == title);
            n.MarkAsRead(Now);
            await db.SaveChangesAsync();
        }

        var history = await Service(actor).GetHistoryAsync(1, 50);

        AnnouncementHistoryItem item = history.Items.Single(i => i.Title == title);
        Assert.Equal(1, item.Recipients);
        Assert.Equal(1, item.Read);
        Assert.True(history.TotalCount >= 1);
    }

    [Fact]
    public async Task AGroupMessageReachesOnlyStoreOwnersOrSellers()
    {
        Guid actor = await AddUserAsync();
        Guid owner = await AddUserAsync();
        Guid seller = await AddUserAsync();
        Guid plain = await AddUserAsync();
        await AddActiveStoreAsync(owner);
        await AddActiveListingAsync(seller);

        await Service(actor).SendAsync(null, AnnouncementAudience.StoreOwners, "Mağazalara", "Yeni qaydalar.", null);
        await Service(actor).SendAsync(null, AnnouncementAudience.Sellers, "Satıcılara", "Yeni tarif.", null);

        await using RentoXDbContext db = database.CreateContext();
        Assert.True(await db.Set<Notification>().AnyAsync(n => n.UserId == owner && n.Title == "Mağazalara"));
        Assert.False(await db.Set<Notification>().AnyAsync(n => n.UserId == seller && n.Title == "Mağazalara"));
        Assert.True(await db.Set<Notification>().AnyAsync(n => n.UserId == seller && n.Title == "Satıcılara"));
        Assert.False(await db.Set<Notification>().AnyAsync(n => n.UserId == owner && n.Title == "Satıcılara"));
        Assert.False(await db.Set<Notification>().AnyAsync(n => n.UserId == plain));
    }

    private async Task AddActiveStoreAsync(Guid ownerId)
    {
        await using RentoXDbContext db = database.CreateContext();
        StoreProfile store = StoreProfile.Create(
            ownerId, "Group test store", "group-test-" + Guid.NewGuid().ToString("N"),
            "A store created for the announcement tests.", "+994500000000", null, null, null, null, null, null);
        store.SetLogo("stores/logo.webp");
        store.SubmitForReview();
        store.Approve();
        db.StoreProfiles.Add(store);
        await db.SaveChangesAsync();
    }

    private async Task AddActiveListingAsync(Guid ownerId)
    {
        await using RentoXDbContext db = database.CreateContext();
        Category category = Category.Create(null, $"ann-{Guid.NewGuid():N}", null, 0);
        category.AddTranslation(CategoryTranslation.Create(category.Id, PreferredLanguage.Azerbaijani, "Test"));
        db.Categories.Add(category);
        Listing listing = Listing.Create(
            ownerId, category.Id, "Bildiriş testi", "Təsvir mətni burada yazılıb.", 50m, "AZN", RentalPeriodUnit.Day);
        listing.MarkAsCreated(Now, ownerId);
        listing.AddImage($"tests/ann-{Guid.NewGuid():N}.jpg", "image/jpeg", 100);
        listing.SubmitForReview();
        listing.Publish(Now, TimeSpan.FromDays(30));
        db.Listings.Add(listing);
        await db.SaveChangesAsync();
    }

    private AnnouncementService Service(Guid actor) =>
        new(database.CreateContext(), new TestClock(), new TestActor(actor));

    private async Task<Guid> AddUserAsync(bool block = false, bool announcements = true)
    {
        await using RentoXDbContext db = database.CreateContext();
        Guid id = Guid.NewGuid();
        string userName = $"ann-{id:N}";
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
        UserProfile profile = UserProfile.Create(id, "Test " + id.ToString("N")[..6], PreferredLanguage.Azerbaijani);
        if (block)
        {
            profile.Block("test");
        }

        profile.SetAnnouncementsEnabled(announcements);

        db.Set<UserProfile>().Add(profile);
        await db.SaveChangesAsync();
        return id;
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class TestActor(Guid userId) : ICurrentUserContext
    {
        public Guid? UserId => userId;

        public bool IsAuthenticated => true;
    }
}
