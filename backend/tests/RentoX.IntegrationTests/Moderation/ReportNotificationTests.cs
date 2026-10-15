using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Notifications;
using RentoX.Application.Support;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Moderation;
using RentoX.Domain.Notifications;
using RentoX.Domain.Support;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Moderation;
using RentoX.Infrastructure.Persistence;
using RentoX.Infrastructure.Support;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Moderation;

/// <summary>The person who reported is told what became of it; the staff see what the person was told.</summary>
public sealed class ReportNotificationTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ADecisionOnAListingReportIsToldToWhoReportedItWithTheNoteOfTheModerator()
    {
        Guid owner = await AddUserAsync("Sahib");
        Guid reporter = await AddUserAsync("Şikayətçi");
        Guid reviewer = await AddUserAsync("Moderator");
        Guid listing = await AddListingAsync(owner);
        var told = new Recorder();
        await using RentoXDbContext db = database.CreateContext();
        var service = new ContentReportService(db, new TestClock(), null, told);

        var report = await service.CreateAsync(reporter, (int)ContentReportTarget.Listing, listing, (int)ContentReportReason.Fraud, null);
        await service.ReviewAsync(reviewer, report!.Id, (int)ContentReportStatus.Resolved, "Elan götürüldü");

        CreateNotificationCommand command = Assert.Single(told.Commands);
        Assert.Equal(reporter, command.UserId);
        Assert.Equal((int)NotificationType.ConversationReportUpdated, command.Type);
        Assert.Equal("Şikayət həll edildi", command.Title);
        Assert.Equal("Elan götürüldü", command.Body);
        Assert.Equal($"/listings/{listing:D}", command.ActionUrl);
    }

    [Fact]
    public async Task TheStaffSeeEveryNotificationAboutARequestWhetherItWasReadAndHowThePushWent()
    {
        Guid user = await AddUserAsync("Müştəri");
        Guid admin = await AddUserAsync("Əməkdaş");
        await using RentoXDbContext db = database.CreateContext();
        var tickets = new SupportTicketService(db, new TestClock(), new Told(), new Actor(admin));
        var created = await tickets.CreateAsync(new CreateSupportTicketCommand(user, 1, "Mövzu", "Salam, kömək lazımdır."));

        Notification read = Notification.Create(user, NotificationType.SupportReply, "Cavab", "Mətn", created.Id, null, Now.AddMinutes(-5));
        read.MarkAsRead(Now.AddMinutes(-4));
        Notification unread = Notification.Create(user, NotificationType.SupportStatusChanged, "Status", "Mətn", created.Id, null, Now);
        db.Set<Notification>().AddRange(read, unread);
        db.Set<PushDevice>().Add(PushDevice.Register(user, PushPlatform.Android, $"device-{Guid.NewGuid():N}", "token-" + Guid.NewGuid().ToString("N"), Now));
        await db.SaveChangesAsync();

        var details = await tickets.GetAdminDetailsAsync(created.Id);

        Assert.NotNull(details?.Notifications);
        Assert.Equal(2, details.Notifications.Count);
        // The newest first.
        Assert.Equal("Status", details.Notifications[0].Title);
        Assert.Null(details.Notifications[0].ReadAtUtc);
        Assert.Equal(Now.AddMinutes(-4), details.Notifications[1].ReadAtUtc);
        Assert.Equal(1, details.UserDevices);
    }

    [Fact]
    public async Task AModeratorCanOpenALiveListingThatWasReportedNotOnlyOneWaitingForReview()
    {
        Guid owner = await AddUserAsync("Sahib");
        Guid listing = await AddListingAsync(owner);
        await using RentoXDbContext db = database.CreateContext();

        var details = await new RentoX.Infrastructure.Listings.ListingModerationQueryService(db)
            .GetByIdAsync(listing, PreferredLanguage.Azerbaijani);

        Assert.NotNull(details);
        Assert.Equal(listing, details.Id);
    }

    // ---------------------------------------------------------------- seeding

    private async Task<Guid> AddUserAsync(string name)
    {
        await using RentoXDbContext db = database.CreateContext();
        Guid id = Guid.NewGuid();
        string userName = $"rn-{id:N}";
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
        Category category = Category.Create(null, $"rn-{Guid.NewGuid():N}", null, 0);
        category.AddTranslation(CategoryTranslation.Create(category.Id, PreferredLanguage.Azerbaijani, "Test"));
        db.Categories.Add(category);
        Listing listing = Listing.Create(
            ownerId, category.Id, "Şikayət edilən elan", "Təsvir mətni burada yazılıb.", 50m, "AZN", RentalPeriodUnit.Day);
        listing.MarkAsCreated(Now, ownerId);
        listing.AddImage($"tests/rn-{Guid.NewGuid():N}.jpg", "image/jpeg", 100);
        listing.SubmitForReview();
        listing.Publish(Now, TimeSpan.FromDays(30));
        db.Listings.Add(listing);
        await db.SaveChangesAsync();
        return listing.Id;
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class Actor(Guid id) : ICurrentUserContext
    {
        public Guid? UserId => id;

        public bool IsAuthenticated => true;
    }

    private class Told : INotificationService
    {
        public virtual Task<NotificationResult> CreateAsync(CreateNotificationCommand command, CancellationToken cancellationToken = default) =>
            Task.FromResult<NotificationResult>(null!);

        public Task<RentoX.Application.Common.PagedResult<NotificationResult>> GetAsync(
            Guid userId, int page, int pageSize, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class Recorder : Told
    {
        public List<CreateNotificationCommand> Commands { get; } = [];

        public override Task<NotificationResult> CreateAsync(CreateNotificationCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            return Task.FromResult<NotificationResult>(null!);
        }
    }
}
