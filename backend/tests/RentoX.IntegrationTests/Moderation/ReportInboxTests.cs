using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Messaging;
using RentoX.Application.Moderation;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Messaging;
using RentoX.Domain.Moderation;
using RentoX.Domain.Stores;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Messaging;
using RentoX.Infrastructure.Moderation;
using RentoX.Infrastructure.Persistence;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Moderation;

/// <summary>The moderators' queue of reports: the order it is worked in, who is named, and how many wait on the same thing.</summary>
public sealed class ReportInboxTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task OpenContentReportsComeOldestFirstWithNamesAndTheNumberWaitingOnTheSameThing()
    {
        Guid owner = await AddUserAsync("Sahib Əliyev");
        Guid one = await AddUserAsync("Birinci Şikayətçi");
        Guid two = await AddUserAsync("İkinci Şikayətçi");
        Guid reviewer = await AddUserAsync("Moderator");
        Guid listing = await AddListingAsync(owner);
        Guid store = await AddStoreAsync(owner);

        Guid oldestOnListing = await AddContentReportAsync(ContentReportTarget.Listing, listing, one, Now.AddDays(-3));
        Guid onStore = await AddContentReportAsync(ContentReportTarget.Store, store, one, Now.AddDays(-2));
        Guid newestOnListing = await AddContentReportAsync(ContentReportTarget.Listing, listing, two, Now.AddDays(-1));
        Guid closed = await AddContentReportAsync(
            ContentReportTarget.Listing, listing, await AddUserAsync("Üçüncü"), Now.AddDays(-5), close: (reviewer, ContentReportStatus.Rejected));

        await using RentoXDbContext db = database.CreateContext();
        var service = new ContentReportService(db, new TestClock());

        var open = await service.GetForAdminAsync((int)ContentReportStatus.Pending, null, 1, 50);
        ContentReportSummaryResult[] mine = open.Items.Where(i => new[] { oldestOnListing, onStore, newestOnListing }.Contains(i.Id)).ToArray();

        // The one that has waited longest is first.
        Assert.Equal(new[] { oldestOnListing, onStore, newestOnListing }, mine.Select(i => i.Id).ToArray());
        Assert.DoesNotContain(open.Items, i => i.Id == closed);

        ContentReportSummaryResult first = mine[0];
        Assert.Equal("Birinci Şikayətçi", first.ReporterName);
        Assert.Equal("Sahib Əliyev", first.OwnerName);
        Assert.Equal(owner, first.OwnerId);
        Assert.Equal(2, first.OpenOnTarget);           // two open reports on this listing
        Assert.Equal(1, mine[1].OpenOnTarget);          // one on the store
        Assert.Equal((int)ListingStatus.Active, first.TargetStatus);
    }

    [Fact]
    public async Task AClosedListIsReadFromTheNewestAndAThingCanBeAskedForItsOwnReports()
    {
        Guid owner = await AddUserAsync("Sahib");
        Guid reporter = await AddUserAsync("Şikayətçi");
        Guid reviewer = await AddUserAsync("Moderator");
        Guid listing = await AddListingAsync(owner);
        Guid other = await AddListingAsync(owner);

        Guid older = await AddContentReportAsync(ContentReportTarget.Listing, listing, reporter, Now.AddDays(-6), close: (reviewer, ContentReportStatus.Resolved));
        Guid newer = await AddContentReportAsync(ContentReportTarget.Listing, listing, await AddUserAsync("Başqa"), Now.AddDays(-2), close: (reviewer, ContentReportStatus.Resolved));
        Guid elsewhere = await AddContentReportAsync(ContentReportTarget.Listing, other, reporter, Now.AddDays(-1), close: (reviewer, ContentReportStatus.Resolved));

        await using RentoXDbContext db = database.CreateContext();
        var service = new ContentReportService(db, new TestClock());

        var closed = await service.GetForAdminAsync((int)ContentReportStatus.Resolved, null, 1, 50);
        Guid[] order = closed.Items.Select(i => i.Id).Where(id => new[] { older, newer, elsewhere }.Contains(id)).ToArray();
        Assert.Equal(new[] { elsewhere, newer, older }, order);

        var onlyThisListing = await service.GetForAdminAsync(null, null, 1, 50, listing);
        Assert.Equal(new[] { newer, older }.Order().ToArray(), onlyThisListing.Items.Select(i => i.Id).Order().ToArray());
        Assert.All(onlyThisListing.Items, i => Assert.Equal(listing, i.TargetId));
    }

    [Fact]
    public async Task TheDetailsNameTheReporterAndTheOwner()
    {
        Guid owner = await AddUserAsync("Mağaza Sahibi");
        Guid reporter = await AddUserAsync("Şikayətçi Adı");
        Guid store = await AddStoreAsync(owner);
        Guid report = await AddContentReportAsync(ContentReportTarget.Store, store, reporter, Now.AddHours(-5));

        await using RentoXDbContext db = database.CreateContext();
        ContentReportDetailsResult? details = await new ContentReportService(db, new TestClock()).GetDetailsAsync(report);

        Assert.NotNull(details);
        Assert.Equal("Şikayətçi Adı", details.ReporterName);
        Assert.Equal("Mağaza Sahibi", details.OwnerName);
        Assert.Equal(1, details.OpenReportsOnTarget);
    }

    [Fact]
    public async Task ConversationReportsAreOrderedLikeTheOthersAndNameBothPeople()
    {
        Guid seller = await AddUserAsync("Satıcı Adı");
        Guid buyer = await AddUserAsync("Alıcı Adı");
        Guid listing = await AddListingAsync(seller);
        Guid conversation = await AddConversationAsync(listing, buyer, seller);
        Guid reviewer = await AddUserAsync("Moderator");

        // One report per person per conversation: the buyer first, the seller later; a closed one is in another chat.
        Guid oldest = await AddChatReportAsync(conversation, buyer, Now.AddDays(-4));
        Guid newest = await AddChatReportAsync(conversation, seller, Now.AddDays(-1));
        Guid otherChat = await AddConversationAsync(listing, await AddUserAsync("Başqa alıcı"), seller);
        Guid done = await AddChatReportAsync(otherChat, seller, Now.AddDays(-9), close: reviewer);

        await using RentoXDbContext db = database.CreateContext();
        var service = new ConversationReportService(db, new TestClock(), new NoFiles(), new NoNotifications());

        var open = await service.GetForAdminAsync(1, 1, 50);
        ConversationReportSummaryResult[] mine = open.Items.Where(i => i.ConversationId == conversation).ToArray();

        Assert.Equal(new[] { oldest, newest }, mine.Select(i => i.Id).ToArray());
        Assert.DoesNotContain(open.Items, i => i.Id == done);
        Assert.Equal("Alıcı Adı", mine[0].ReporterName);
        Assert.Equal("Satıcı Adı", mine[0].OtherUserName);
        Assert.Equal(2, mine[0].OpenOnConversation);

        ConversationReportDetailsResult? details = await service.GetDetailsAsync(oldest);
        Assert.NotNull(details);
        Assert.Equal("Alıcı Adı", details.ReporterName);
        Assert.Equal("Alıcı Adı", details.BuyerName);
        Assert.Equal("Satıcı Adı", details.SellerName);
    }

    // ---------------------------------------------------------------- seeding

    private async Task<Guid> AddContentReportAsync(
        ContentReportTarget target, Guid targetId, Guid reporter, DateTimeOffset at,
        (Guid Reviewer, ContentReportStatus Status)? close = null)
    {
        await using RentoXDbContext db = database.CreateContext();
        ContentReport report = ContentReport.Create(target, targetId, reporter, ContentReportReason.Fraud, null, at);
        if (close is { } c)
        {
            report.Review(c.Status, c.Reviewer, "Qərar qeydi", at.AddHours(1));
        }

        db.Set<ContentReport>().Add(report);
        await db.SaveChangesAsync();
        return report.Id;
    }

    private async Task<Guid> AddChatReportAsync(Guid conversationId, Guid reporter, DateTimeOffset at, Guid? close = null)
    {
        await using RentoXDbContext db = database.CreateContext();
        ConversationReport report = ConversationReport.Create(
            conversationId, reporter, ConversationReportReason.Harassment, null, null, at);
        if (close is { } reviewer)
        {
            report.Review(ConversationReportStatus.Resolved, reviewer, "Həll olundu", at.AddHours(1));
        }

        db.Set<ConversationReport>().Add(report);
        await db.SaveChangesAsync();
        return report.Id;
    }

    private async Task<Guid> AddUserAsync(string name)
    {
        await using RentoXDbContext db = database.CreateContext();
        Guid id = Guid.NewGuid();
        string userName = $"inbox-{id:N}";
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
        db.Set<UserProfile>().Add(UserProfile.Create(id, name, PreferredLanguage.Azerbaijani));
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<Guid> AddListingAsync(Guid ownerId)
    {
        await using RentoXDbContext db = database.CreateContext();
        Category category = Category.Create(null, $"inbox-{Guid.NewGuid():N}", null, 0);
        category.AddTranslation(CategoryTranslation.Create(category.Id, PreferredLanguage.Azerbaijani, "Test"));
        db.Categories.Add(category);
        Listing listing = Listing.Create(
            ownerId, category.Id, "Şikayət edilən elan", "Təsvir mətni burada yazılıb.", 50m, "AZN", RentalPeriodUnit.Day);
        listing.MarkAsCreated(Now, ownerId);
        listing.AddImage($"tests/inbox-{Guid.NewGuid():N}.jpg", "image/jpeg", 100);
        listing.SubmitForReview();
        listing.Publish(Now, TimeSpan.FromDays(30));
        db.Listings.Add(listing);
        await db.SaveChangesAsync();
        return listing.Id;
    }

    private async Task<Guid> AddStoreAsync(Guid ownerId)
    {
        await using RentoXDbContext db = database.CreateContext();
        StoreProfile store = StoreProfile.Create(
            ownerId, "Şikayət edilən mağaza", "inbox-" + Guid.NewGuid().ToString("N"),
            "A store created for the report queue tests.", "+994500000000", null, null, null, null, null, null);
        store.SetLogo("stores/logo.webp");
        store.SubmitForReview();
        store.Approve();
        db.StoreProfiles.Add(store);
        await db.SaveChangesAsync();
        return store.Id;
    }

    private async Task<Guid> AddConversationAsync(Guid listingId, Guid buyerId, Guid sellerId)
    {
        await using RentoXDbContext db = database.CreateContext();
        Conversation conversation = Conversation.Create(listingId, buyerId, sellerId, Now.AddDays(-10));
        db.Set<Conversation>().Add(conversation);
        await db.SaveChangesAsync();
        return conversation.Id;
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class NoNotifications : RentoX.Application.Notifications.INotificationService
    {
        public Task<RentoX.Application.Notifications.NotificationResult> CreateAsync(
            RentoX.Application.Notifications.CreateNotificationCommand command, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<RentoX.Application.Common.PagedResult<RentoX.Application.Notifications.NotificationResult>> GetAsync(
            Guid userId, int page, int pageSize, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class NoFiles : RentoX.Application.Files.IFileStorage
    {
        public Task<RentoX.Application.Files.StoredFileResult> SaveAsync(
            RentoX.Application.Files.FileStorageArea area, Stream content, string contentType, string extension,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream?>(null);

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
