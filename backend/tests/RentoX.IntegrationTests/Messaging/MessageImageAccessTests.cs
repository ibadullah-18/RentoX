using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Files;
using RentoX.Application.Messaging;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Messaging;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Messaging;
using RentoX.Infrastructure.Persistence;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Messaging;

public sealed class MessageImageAccessTests(
    AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BothParticipantsCanOpenTheImage(bool useBuyer)
    {
        SeedData data = await SeedAsync();

        await using RentoXDbContext db = database.CreateContext();
        StorageSpy storage = new();
        MessageImageService service = CreateService(db, storage);

        Guid userId = useBuyer ? data.BuyerId : data.SellerId;

        MessageImageDownload? result = await service.OpenImageAsync(
            userId,
            data.ConversationId,
            data.ImageId);

        using Stream? content = result?.Content;

        Assert.NotNull(result);
        Assert.NotNull(content);
        Assert.Equal("image/png", result.ContentType);
        Assert.Equal(1, storage.OpenCount);
        Assert.Equal(data.StorageKey, storage.LastOpenedKey);

        using MemoryStream output = new();
        await content.CopyToAsync(output);

        Assert.Equal(
            "private-image",
            Encoding.UTF8.GetString(output.ToArray()));
    }

    [Theory]
    [InlineData("outsider")]
    [InlineData("otherConversation")]
    [InlineData("missingImage")]
    [InlineData("missingConversation")]
    [InlineData("emptyUser")]
    public async Task RejectedDownloadNeverOpensStorage(string scenario)
    {
        SeedData data = await SeedAsync();

        Guid userId = data.BuyerId;
        Guid conversationId = data.ConversationId;
        Guid imageId = data.ImageId;

        switch (scenario)
        {
            case "outsider":
                userId = data.OutsiderId;
                break;

            case "otherConversation":
                // The user belongs to this conversation too,
                // but the requested image belongs to another one.
                conversationId = data.OtherConversationId;
                break;

            case "missingImage":
                imageId = Guid.NewGuid();
                break;

            case "missingConversation":
                conversationId = Guid.NewGuid();
                break;

            case "emptyUser":
                userId = Guid.Empty;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(scenario));
        }

        await using RentoXDbContext db = database.CreateContext();
        StorageSpy storage = new();
        MessageImageService service = CreateService(db, storage);

        MessageImageDownload? result = await service.OpenImageAsync(
            userId,
            conversationId,
            imageId);

        using Stream? content = result?.Content;

        Assert.Null(result);
        Assert.Equal(0, storage.OpenCount);
        Assert.Null(storage.LastOpenedKey);
        Assert.Equal(0, storage.SaveCount);
        Assert.Equal(0, storage.DeleteCount);
    }

    [Fact]
    public async Task MissingPhysicalFileReturnsNullForParticipant()
    {
        SeedData data = await SeedAsync();

        await using RentoXDbContext db = database.CreateContext();

        StorageSpy storage = new()
        {
            FileExists = false
        };

        MessageImageService service = CreateService(db, storage);

        MessageImageDownload? result = await service.OpenImageAsync(
            data.SellerId,
            data.ConversationId,
            data.ImageId);

        using Stream? content = result?.Content;

        Assert.Null(result);
        Assert.Equal(1, storage.OpenCount);
        Assert.Equal(data.StorageKey, storage.LastOpenedKey);
        Assert.Equal(0, storage.SaveCount);
        Assert.Equal(0, storage.DeleteCount);
    }

    [Fact]
    public async Task OutsiderCannotUploadIntoAnotherConversation()
    {
        SeedData data = await SeedAsync();

        await using RentoXDbContext db = database.CreateContext();
        StorageSpy storage = new();
        MessageImageService service = CreateService(db, storage);

        // The access check must happen before any image is stored.
        byte[] payload = Encoding.UTF8.GetBytes("not-an-image");
        using MemoryStream content = new(payload, writable: false);

        List<MessageImageUpload> images =
        [
            new MessageImageUpload(
                content,
                "image/png",
                payload.LongLength)
        ];

        MessageResult? result = await service.SendImagesAsync(
            data.OutsiderId,
            data.ConversationId,
            "Unauthorized upload",
            images);

        Assert.Null(result);
        Assert.Equal(0, storage.OpenCount);
        Assert.Equal(0, storage.SaveCount);
        Assert.Equal(0, storage.DeleteCount);
        Assert.Equal(0L, content.Position);
    }

    private async Task<SeedData> SeedAsync()
    {
        await using RentoXDbContext db = database.CreateContext();

        DateTimeOffset now = DateTimeOffset.UtcNow;

        AppUser buyer = CreateUser(now);
        AppUser seller = CreateUser(now);
        AppUser outsider = CreateUser(now);

        Category category = Category.Create(
            null,
            $"image-access-{Guid.NewGuid():N}",
            null,
            0);

        Listing listing = Listing.Create(
            seller.Id,
            category.Id,
            "Image access test listing",
            "A listing used only by isolated message image access tests.",
            100m,
            "AZN",
            (RentalPeriodUnit)2);

        Listing otherListing = Listing.Create(
            seller.Id,
            category.Id,
            "Second image access test listing",
            "A second listing used to test conversation boundaries.",
            120m,
            "AZN",
            (RentalPeriodUnit)2);

        listing.MarkAsCreated(now, seller.Id);
        otherListing.MarkAsCreated(now, seller.Id);

        Conversation conversation = Conversation.Create(
            listing.Id,
            buyer.Id,
            seller.Id,
            now);

        Conversation otherConversation = Conversation.Create(
            otherListing.Id,
            buyer.Id,
            seller.Id,
            now);

        string storageKey =
            $"chat-attachments/{Guid.NewGuid():N}.png";

        List<MessageImageFile> files =
        [
            new MessageImageFile(
                storageKey,
                "image/png",
                Encoding.UTF8.GetByteCount("private-image"))
        ];

        Message message = Message.CreateWithImages(
            conversation.Id,
            buyer.Id,
            "Private image",
            now,
            files);

        db.Users.AddRange(buyer, seller, outsider);
        db.Categories.Add(category);
        db.Listings.AddRange(listing, otherListing);

        db.Conversations.AddRange(
            conversation,
            otherConversation);

        db.Messages.Add(message);

        await db.SaveChangesAsync();

        MessageImage image = Assert.Single(message.Images);

        return new SeedData(
            buyer.Id,
            seller.Id,
            outsider.Id,
            conversation.Id,
            otherConversation.Id,
            image.Id,
            storageKey);
    }

    private static AppUser CreateUser(DateTimeOffset now)
    {
        Guid id = Guid.NewGuid();
        string name = $"image-test-{id:N}";

        return new AppUser
        {
            Id = id,
            UserName = name,
            NormalizedUserName = name.ToUpperInvariant(),
            RegisteredAtUtc = now,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };
    }

    private static MessageImageService CreateService(
        RentoXDbContext db,
        StorageSpy storage)
    {
        return new MessageImageService(
            db,
            storage,
            new TestClock(),
            new UnexpectedEventPublisher(),
            NullLogger<MessageImageService>.Instance);
    }

    private sealed record SeedData(
        Guid BuyerId,
        Guid SellerId,
        Guid OutsiderId,
        Guid ConversationId,
        Guid OtherConversationId,
        Guid ImageId,
        string StorageKey);

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow { get; } =
            DateTimeOffset.UtcNow;
    }

    private sealed class StorageSpy : IFileStorage
    {
        public int OpenCount { get; private set; }

        public int SaveCount { get; private set; }

        public int DeleteCount { get; private set; }

        public string? LastOpenedKey { get; private set; }

        public bool FileExists { get; set; } = true;

        public Task<Stream?> OpenReadAsync(
            string storageKey,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            OpenCount++;
            LastOpenedKey = storageKey;

            Stream? stream = FileExists
                ? new MemoryStream(
                    Encoding.UTF8.GetBytes("private-image"),
                    writable: false)
                : null;

            return Task.FromResult(stream);
        }

        public Task<StoredFileResult> SaveAsync(
            FileStorageArea area,
            Stream content,
            string contentType,
            string extension,
            CancellationToken cancellationToken = default)
        {
            SaveCount++;

            throw new InvalidOperationException(
                "These access tests must not save files.");
        }

        public Task DeleteAsync(
            string storageKey,
            CancellationToken cancellationToken = default)
        {
            DeleteCount++;

            throw new InvalidOperationException(
                "These access tests must not delete files.");
        }
    }

    private sealed class UnexpectedEventPublisher
        : IConversationEventPublisher
    {
        public Task MessageCreatedAsync(
            Guid buyerId,
            Guid sellerId,
            MessageResult message,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "These access tests must not publish message events.");
        }

        public Task MessagesReadAsync(
            Guid buyerId,
            Guid sellerId,
            Guid conversationId,
            Guid readByUserId,
            DateTimeOffset readAtUtc,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "These access tests must not publish read events.");
        }
    }
}
