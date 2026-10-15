using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Messaging;
using RentoX.Application.Notifications;
using RentoX.Application.Support;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Messaging;
using RentoX.Domain.Support;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Messaging;
using RentoX.Infrastructure.Moderation;
using RentoX.Infrastructure.Persistence;
using RentoX.Infrastructure.Support;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Moderation;

/// <summary>What is told to the people looking at the screen: the owner of a request and the staff.</summary>
public sealed class LiveSupportEventsTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ANewRequestIsToldToTheStaffWithTheNameOfThePerson()
    {
        Guid user = await AddUserAsync("Müştəri Adı");
        var live = new Recorder();
        await using RentoXDbContext db = database.CreateContext();

        var ticket = await Tickets(db, live, Guid.NewGuid())
            .CreateAsync(new CreateSupportTicketCommand(user, 1, "Canlı mövzu", "Salam, kömək lazımdır."));

        Assert.Equal($"ticket:{ticket.Id}:{user}:Canlı mövzu:Müştəri Adı", Assert.Single(live.Lines));
    }

    [Fact]
    public async Task AMessageFromThePersonIsToldWithTheirNameAndOneFromTheStaffWithTheirs()
    {
        Guid user = await AddUserAsync("Müştəri Adı");
        Guid admin = await AddUserAsync("Dəstək Əməkdaşı");
        var live = new Recorder();
        await using RentoXDbContext db = database.CreateContext();
        var tickets = Tickets(db, live, admin);
        var created = await tickets.CreateAsync(new CreateSupportTicketCommand(user, 1, "Mövzu", "Salam, kömək lazımdır."));
        live.Lines.Clear();

        await tickets.AddUserMessageAsync(user, created.Id, "Əlavə məlumat");
        await tickets.AddAdminMessageAsync(admin, created.Id, "Baxırıq");

        Assert.Contains(live.Lines, l => l.StartsWith($"message:{created.Id}:{user}:False:Müştəri Adı:Əlavə məlumat", StringComparison.Ordinal));
        Assert.Contains(live.Lines, l => l.StartsWith($"message:{created.Id}:{user}:True:Dəstək Əməkdaşı:Baxırıq", StringComparison.Ordinal));
        // The answer moved a new request on: that is told as well.
        Assert.Contains(live.Lines, l => l.StartsWith($"changed:{created.Id}:{user}:2:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AChangeOfStatusOrPriorityIsToldToTheOwnerAndTheStaffButNothingWhenNothingChanged()
    {
        Guid user = await AddUserAsync("Müştəri Adı");
        Guid admin = await AddUserAsync("Dəstək Əməkdaşı");
        var live = new Recorder();
        await using RentoXDbContext db = database.CreateContext();
        var tickets = Tickets(db, live, admin);
        var created = await tickets.CreateAsync(new CreateSupportTicketCommand(user, 1, "Mövzu", "Salam, kömək lazımdır."));
        live.Lines.Clear();

        await tickets.ChangePriorityAsync(admin, created.Id, (int)SupportTicketPriority.Urgent);
        await tickets.ChangeStatusAsync(admin, created.Id, (int)SupportTicketStatus.Resolved);
        int told = live.Lines.Count;
        await tickets.ChangeStatusAsync(admin, created.Id, (int)SupportTicketStatus.Resolved);

        Assert.Equal(2, told);
        Assert.Equal(told, live.Lines.Count);
        Assert.Equal($"changed:{created.Id}:{user}:3:4", live.Lines[^1]);
    }

    [Fact]
    public async Task ANewReportOfEitherKindIsToldToTheReviewers()
    {
        Guid owner = await AddUserAsync("Sahib");
        Guid reporter = await AddUserAsync("Şikayətçi");
        Guid listing = await AddListingAsync(owner);
        Guid conversation = await AddConversationAsync(listing, reporter, owner);
        var live = new Recorder();
        await using RentoXDbContext db = database.CreateContext();

        var content = await new ContentReportService(db, new TestClock(), live)
            .CreateAsync(reporter, (int)RentoX.Domain.Moderation.ContentReportTarget.Listing, listing, (int)RentoX.Domain.Moderation.ContentReportReason.Fraud, null);
        var chat = await new ConversationReportService(db, new TestClock(), new NoFiles(), new NoNotifications(), live)
            .CreateAsync(new CreateConversationReportCommand(reporter, conversation, (int)ConversationReportReason.Harassment, null, null));

        Assert.Equal(new[] { $"report:content:{content!.Id}", $"report:chat:{chat!.Id}" }, live.Lines);
    }

    [Fact]
    public async Task TheOwnerOfARequestIsFoundByItsIdAndAMissingOneGivesNothing()
    {
        Guid user = await AddUserAsync("Müştəri Adı");
        await using RentoXDbContext db = database.CreateContext();
        var tickets = Tickets(db, new Recorder(), Guid.NewGuid());
        var created = await tickets.CreateAsync(new CreateSupportTicketCommand(user, 1, "Mövzu", "Salam, kömək lazımdır."));

        Assert.Equal(user, await tickets.GetOwnerIdAsync(created.Id));
        Assert.Null(await tickets.GetOwnerIdAsync(Guid.NewGuid()));
    }

    // ---------------------------------------------------------------- seeding

    private static SupportTicketService Tickets(RentoXDbContext db, Recorder live, Guid actor) =>
        new(db, new TestClock(), new Quiet(), new Actor(actor), live);

    private async Task<Guid> AddUserAsync(string name)
    {
        await using RentoXDbContext db = database.CreateContext();
        Guid id = Guid.NewGuid();
        string userName = $"live-{id:N}";
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
        Category category = Category.Create(null, $"live-{Guid.NewGuid():N}", null, 0);
        category.AddTranslation(CategoryTranslation.Create(category.Id, PreferredLanguage.Azerbaijani, "Test"));
        db.Categories.Add(category);
        Listing listing = Listing.Create(
            ownerId, category.Id, "Şikayət edilən elan", "Təsvir mətni burada yazılıb.", 50m, "AZN", RentalPeriodUnit.Day);
        listing.MarkAsCreated(Now, ownerId);
        listing.AddImage($"tests/live-{Guid.NewGuid():N}.jpg", "image/jpeg", 100);
        listing.SubmitForReview();
        listing.Publish(Now, TimeSpan.FromDays(30));
        db.Listings.Add(listing);
        await db.SaveChangesAsync();
        return listing.Id;
    }

    private async Task<Guid> AddConversationAsync(Guid listingId, Guid buyerId, Guid sellerId)
    {
        await using RentoXDbContext db = database.CreateContext();
        Conversation conversation = Conversation.Create(listingId, buyerId, sellerId, Now.AddDays(-10));
        db.Set<Conversation>().Add(conversation);
        await db.SaveChangesAsync();
        return conversation.Id;
    }

    private sealed class Recorder : ISupportEventPublisher
    {
        public List<string> Lines { get; } = [];

        public Task MessageAddedAsync(Guid ticketId, Guid ownerId, SupportTicketMessageResult message, CancellationToken cancellationToken = default)
        {
            Lines.Add($"message:{ticketId}:{ownerId}:{message.IsAdmin}:{message.SenderName}:{message.Body}");
            return Task.CompletedTask;
        }

        public Task TicketCreatedAsync(Guid ticketId, Guid ownerId, string subject, string? userName, CancellationToken cancellationToken = default)
        {
            Lines.Add($"ticket:{ticketId}:{ownerId}:{subject}:{userName}");
            return Task.CompletedTask;
        }

        public Task TicketChangedAsync(Guid ticketId, Guid ownerId, int status, int priority, CancellationToken cancellationToken = default)
        {
            Lines.Add($"changed:{ticketId}:{ownerId}:{status}:{priority}");
            return Task.CompletedTask;
        }

        public Task ReportCreatedAsync(string kind, Guid reportId, CancellationToken cancellationToken = default)
        {
            Lines.Add($"report:{kind}:{reportId}");
            return Task.CompletedTask;
        }
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

    private sealed class Quiet : INotificationService
    {
        public Task<NotificationResult> CreateAsync(CreateNotificationCommand command, CancellationToken cancellationToken = default) =>
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

    private sealed class NoNotifications : INotificationService
    {
        public Task<NotificationResult> CreateAsync(CreateNotificationCommand command, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

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

    private sealed class NoFiles : RentoX.Application.Files.IFileStorage
    {
        public Task<RentoX.Application.Files.StoredFileResult> SaveAsync(
            RentoX.Application.Files.FileStorageArea area, Stream content, string contentType, string extension,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream?>(null);

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
