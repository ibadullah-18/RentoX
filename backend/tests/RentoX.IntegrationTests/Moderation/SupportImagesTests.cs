using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Files;
using RentoX.Application.Notifications;
using RentoX.Application.Support;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Support;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Persistence;
using RentoX.Infrastructure.Support;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Moderation;

/// <summary>Photos in a support request: the person and the staff can both send them, and only they can open them.</summary>
public sealed class SupportImagesTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 9, 0, 0, TimeSpan.Zero);

    private static readonly byte[] Png =
        [137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82, 0, 0, 0, 1, 0, 0, 0, 1, 8, 6, 0, 0, 0];

    private static SupportImageUpload Upload(string type = "image/png", byte[]? bytes = null) =>
        new(new MemoryStream(bytes ?? Png), type, (bytes ?? Png).Length);

    [Fact]
    public async Task TheStaffAndThePersonBothSendPhotosAloneOrWithWordsAndEachSideSeesThemInTheThread()
    {
        Guid user = await AddUserAsync("Müştəri");
        Guid admin = await AddUserAsync("Əməkdaş");
        MemoryStorage storage = new();
        await using RentoXDbContext db = database.CreateContext();
        SupportTicketService tickets = new(db, new Clock(), new Told(), new Actor(admin), null, storage);

        var created = await tickets.CreateAsync(new CreateSupportTicketCommand(user, 1, "Ödəniş", "Pul çıxdı, elan yoxdur."));

        // The person sends a screenshot with no words at all.
        SupportTicketMessageResult? first = await tickets.AddUserMessageWithImagesAsync(
            user, created.Id, string.Empty, [Upload(), Upload()]);
        Assert.NotNull(first);
        Assert.Equal(string.Empty, first.Body);
        Assert.Equal(2, first.Images!.Count);

        // The staff answers with words and a photo.
        SupportTicketMessageResult? answer = await tickets.AddAdminMessageWithImagesAsync(
            admin, created.Id, "Çek göndərdik.", [Upload()]);
        Assert.Single(answer!.Images!);
        Assert.True(answer.IsAdmin);

        var details = await tickets.GetMineDetailsAsync(user, created.Id);
        Assert.Equal(3, details!.Messages.Count);
        // (The clock is fixed in this test, so the order of the three is not asserted, only what each one has.)
        Assert.Contains(details.Messages, m => !m.IsAdmin && m.Images is { Count: 2 } && m.Body.Length == 0);
        Assert.Contains(details.Messages, m => m.IsAdmin && m.Images is { Count: 1 } && m.Body == "Çek göndərdik.");
        Assert.Equal(3, storage.Files.Count);

        var adminView = await tickets.GetAdminDetailsAsync(created.Id);
        Assert.Equal(3, adminView!.Messages.Sum(m => m.Images?.Count ?? 0));
    }

    [Fact]
    public async Task OnlyTheOwnerAndTheStaffCanOpenAPhotoAndNotAStrangerOrAnotherRequest()
    {
        Guid user = await AddUserAsync("Sahib");
        Guid other = await AddUserAsync("Başqası");
        Guid admin = await AddUserAsync("Əməkdaş");
        MemoryStorage storage = new();
        await using RentoXDbContext db = database.CreateContext();
        SupportTicketService tickets = new(db, new Clock(), new Told(), new Actor(admin), null, storage);

        var created = await tickets.CreateAsync(new CreateSupportTicketCommand(user, 2, "Hesab", "Kömək edin."));
        var message = await tickets.AddUserMessageWithImagesAsync(user, created.Id, "Bax", [Upload()]);
        Guid imageId = message!.Images![0].Id;

        Assert.NotNull(await tickets.OpenImageAsync(created.Id, imageId, user));
        Assert.NotNull(await tickets.OpenImageAsync(created.Id, imageId, null));
        Assert.Null(await tickets.OpenImageAsync(created.Id, imageId, other));

        var another = await tickets.CreateAsync(new CreateSupportTicketCommand(user, 2, "Başqa", "Başqa məsələ."));
        Assert.Null(await tickets.OpenImageAsync(another.Id, imageId, user));
    }

    [Fact]
    public async Task WrongPhotosAreRefusedAndNothingStaysInTheStorage()
    {
        Guid user = await AddUserAsync("Müştəri");
        Guid admin = await AddUserAsync("Əməkdaş");
        MemoryStorage storage = new();
        await using RentoXDbContext db = database.CreateContext();
        SupportTicketService tickets = new(db, new Clock(), new Told(), new Actor(admin), null, storage);
        var created = await tickets.CreateAsync(new CreateSupportTicketCommand(user, 1, "Mövzu", "Salam."));

        // Six photos, a file that is not a picture, a type that does not match, nothing at all.
        await Assert.ThrowsAsync<DomainException>(() => tickets.AddUserMessageWithImagesAsync(
            user, created.Id, "x", Enumerable.Range(0, 6).Select(_ => Upload()).ToArray()));
        await Assert.ThrowsAsync<DomainException>(() => tickets.AddUserMessageWithImagesAsync(
            user, created.Id, "x", [Upload(bytes: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10])]));
        await Assert.ThrowsAsync<DomainException>(() => tickets.AddUserMessageWithImagesAsync(
            user, created.Id, "x", [Upload("image/jpeg")]));
        await Assert.ThrowsAsync<DomainException>(() => tickets.AddUserMessageWithImagesAsync(
            user, created.Id, string.Empty, null));

        // A good photo first, then a bad one: the good one is taken back too.
        await Assert.ThrowsAsync<DomainException>(() => tickets.AddUserMessageWithImagesAsync(
            user, created.Id, "x", [Upload(), Upload("image/jpeg")]));
        Assert.Empty(storage.Files);

        // Someone else's request: nothing is stored either.
        Guid stranger = await AddUserAsync("Yad");
        Assert.Null(await tickets.AddUserMessageWithImagesAsync(stranger, created.Id, "x", [Upload()]));
        Assert.Empty(storage.Files);
    }

    private async Task<Guid> AddUserAsync(string name)
    {
        await using RentoXDbContext db = database.CreateContext();
        Guid id = Guid.NewGuid();
        string userName = $"si-{id:N}";
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

    private sealed class Clock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class Actor(Guid id) : ICurrentUserContext
    {
        public Guid? UserId => id;

        public bool IsAuthenticated => true;
    }

    private sealed class Told : INotificationService
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

    private sealed class MemoryStorage : IFileStorage
    {
        public Dictionary<string, byte[]> Files { get; } = [];

        public async Task<StoredFileResult> SaveAsync(
            FileStorageArea area, Stream content, string contentType, string extension, CancellationToken cancellationToken = default)
        {
            using MemoryStream copy = new();
            await content.CopyToAsync(copy, cancellationToken);
            string key = $"support-attachments/{Guid.NewGuid():N}{extension}";
            Files[key] = copy.ToArray();
            return new StoredFileResult(key, contentType, copy.Length);
        }

        public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream?>(Files.TryGetValue(storageKey, out byte[]? data) ? new MemoryStream(data) : null);

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            Files.Remove(storageKey);
            return Task.CompletedTask;
        }
    }
}
