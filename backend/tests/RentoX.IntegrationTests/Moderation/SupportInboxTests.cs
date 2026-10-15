using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Notifications;
using RentoX.Domain.Support;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Persistence;
using RentoX.Infrastructure.Support;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Moderation;

/// <summary>The support queue: open first, the ones waiting for us first, the most urgent, the longest waiting.</summary>
public sealed class SupportInboxTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    private static readonly string[] WhoWrote = ["Müştəri Adı", "Dəstək Əməkdaşı"];

    private static readonly bool[] WhoIsStaff = [false, true];

    [Fact]
    public async Task OpenRequestsWaitingForUsComeFirstThenByUrgencyThenTheLongestWaitingAndClosedOnesLast()
    {
        Guid user = await AddUserAsync("Müştəri Adı");
        Guid admin = await AddUserAsync("Dəstək Əməkdaşı");

        Guid repliedLong = await AddTicketAsync(user, "Cavab verilib", Now.AddDays(-5), adminReplyAt: Now.AddDays(-4), admin: admin);
        Guid waitingOld = await AddTicketAsync(user, "Köhnə sual", Now.AddDays(-3));
        Guid waitingUrgent = await AddTicketAsync(user, "Təcili sual", Now.AddDays(-1), priority: SupportTicketPriority.Urgent);
        Guid waitingNew = await AddTicketAsync(user, "Yeni sual", Now.AddHours(-2));
        Guid resolved = await AddTicketAsync(user, "Həll olunub", Now.AddDays(-6), status: SupportTicketStatus.Resolved);

        await using RentoXDbContext db = database.CreateContext();
        var service = Service(db);

        var all = await service.GetForAdminAsync(null, null, null, 1, 50);
        Guid[] mine = all.Items.Select(i => i.Id)
            .Where(id => new[] { repliedLong, waitingOld, waitingUrgent, waitingNew, resolved }.Contains(id))
            .ToArray();

        // Waiting for us: urgent first, then the older of the rest; then the one that waits for the user; the closed one last.
        Assert.Equal(new[] { waitingUrgent, waitingOld, waitingNew, repliedLong, resolved }, mine);

        var open = await service.GetForAdminAsync(null, null, null, 1, 50, openOnly: true);
        Assert.DoesNotContain(open.Items, i => i.Id == resolved);
        Assert.Contains(open.Items, i => i.Id == repliedLong);
    }

    [Fact]
    public async Task EachRowKnowsWhoWroteAndWhoSpokeLastAndShortensTheLastMessage()
    {
        Guid user = await AddUserAsync("Müştəri Adı");
        Guid admin = await AddUserAsync("Dəstək Əməkdaşı");
        string longText = string.Join(' ', Enumerable.Repeat("uzun cavab mətni", 30));
        Guid ticket = await AddTicketAsync(user, "Mövzu", Now.AddDays(-2), adminReplyAt: Now.AddDays(-1), admin: admin, adminText: longText);

        await using RentoXDbContext db = database.CreateContext();
        var row = (await Service(db).GetForAdminAsync(null, null, null, 1, 50)).Items.Single(i => i.Id == ticket);

        Assert.Equal("Müştəri Adı", row.UserName);
        Assert.True(row.LastMessageIsAdmin);
        Assert.Equal(2, row.MessageCount);
        Assert.NotNull(row.LastMessagePreview);
        Assert.True(row.LastMessagePreview!.Length <= 141);
        Assert.EndsWith("…", row.LastMessagePreview);
        Assert.DoesNotContain('\n', row.LastMessagePreview);
    }

    [Fact]
    public async Task AClosedListIsReadFromTheNewest()
    {
        Guid user = await AddUserAsync("Müştəri");
        Guid older = await AddTicketAsync(user, "Əvvəlki", Now.AddDays(-9), status: SupportTicketStatus.Resolved);
        Guid newer = await AddTicketAsync(user, "Sonrakı", Now.AddDays(-2), status: SupportTicketStatus.Resolved);

        await using RentoXDbContext db = database.CreateContext();
        var closed = await Service(db).GetForAdminAsync((int)SupportTicketStatus.Resolved, null, null, 1, 50);
        Guid[] order = closed.Items.Select(i => i.Id).Where(id => id == older || id == newer).ToArray();

        Assert.Equal(new[] { newer, older }, order);
    }

    [Fact]
    public async Task TheDetailsNameThePersonTheirPhoneAndEveryoneWhoWroteAndCountTheirOtherRequests()
    {
        Guid user = await AddUserAsync("Müştəri Adı", phone: "+994501112233");
        Guid admin = await AddUserAsync("Dəstək Əməkdaşı");
        Guid ticket = await AddTicketAsync(user, "Mövzu", Now.AddDays(-2), adminReplyAt: Now.AddDays(-1), admin: admin);
        await AddTicketAsync(user, "Başqa", Now.AddDays(-4));
        await AddTicketAsync(user, "Daha biri", Now.AddDays(-5));

        await using RentoXDbContext db = database.CreateContext();
        var details = await Service(db).GetAdminDetailsAsync(ticket);

        Assert.NotNull(details);
        Assert.Equal("Müştəri Adı", details.UserName);
        Assert.Equal("+994501112233", details.UserPhone);
        Assert.Equal(2, details.OtherTickets);
        Assert.Equal(WhoWrote, details.Messages.Select(m => m.SenderName).ToArray());
        Assert.Equal(WhoIsStaff, details.Messages.Select(m => m.IsAdmin).ToArray());
    }

    // ---------------------------------------------------------------- seeding

    private async Task<Guid> AddTicketAsync(
        Guid user, string subject, DateTimeOffset created,
        SupportTicketPriority priority = SupportTicketPriority.Normal,
        SupportTicketStatus status = SupportTicketStatus.Open,
        DateTimeOffset? adminReplyAt = null, Guid? admin = null, string adminText = "Cavab yazdıq, baxın.")
    {
        await using RentoXDbContext db = database.CreateContext();
        SupportTicket ticket = SupportTicket.Create(
            user, SupportTicketCategory.General, subject, "Salam, mənə kömək lazımdır.", created);
        if (adminReplyAt is { } at)
        {
            ticket.AddAdminMessage(admin!.Value, adminText, at);
        }

        if (priority != SupportTicketPriority.Normal)
        {
            ticket.ChangePriority(priority, created.AddMinutes(1));
        }

        if (status != SupportTicketStatus.Open)
        {
            ticket.ChangeStatus(status, created.AddMinutes(2));
        }

        db.Set<SupportTicket>().Add(ticket);
        await db.SaveChangesAsync();
        return ticket.Id;
    }

    private async Task<Guid> AddUserAsync(string name, string? phone = null)
    {
        await using RentoXDbContext db = database.CreateContext();
        Guid id = Guid.NewGuid();
        string userName = $"sup-{id:N}";
        db.Users.Add(new AppUser
        {
            Id = id,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            PhoneNumber = phone ?? "+99450" + Random.Shared.Next(1000000, 9999999),
            RegisteredAtUtc = Now,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        });
        db.Set<UserProfile>().Add(UserProfile.Create(id, name, PreferredLanguage.Azerbaijani));
        await db.SaveChangesAsync();
        return id;
    }

    private static SupportTicketService Service(RentoXDbContext db) =>
        new(db, new TestClock(), new NoNotifications(), new TestActor());

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class TestActor : ICurrentUserContext
    {
        public Guid? UserId => Guid.NewGuid();

        public bool IsAuthenticated => true;
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
}
