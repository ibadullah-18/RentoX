using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Files;
using RentoX.Application.Listings;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Listings;
using RentoX.Infrastructure.Persistence;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Listings;

public sealed class ListingImageAccessTests(
    AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset TestTime =
        new(2026, 10, 7, 8, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(ListingStatus.Draft, "anonymous", false)]
    [InlineData(ListingStatus.PendingReview, "anonymous", false)]
    [InlineData(ListingStatus.Active, "anonymous", true)]
    [InlineData(ListingStatus.Rejected, "anonymous", false)]
    [InlineData(ListingStatus.Expired, "anonymous", false)]
    [InlineData(ListingStatus.Deactivated, "anonymous", false)]
    [InlineData(ListingStatus.Deleted, "anonymous", false)]
    [InlineData(ListingStatus.PaymentRequired, "anonymous", false)]
    [InlineData(ListingStatus.Draft, "owner", true)]
    [InlineData(ListingStatus.Expired, "owner", true)]
    [InlineData(ListingStatus.Deleted, "owner", false)]
    [InlineData(ListingStatus.Draft, "moderator", true)]
    [InlineData(ListingStatus.Deleted, "moderator", false)]
    [InlineData(ListingStatus.Draft, "other", false)]
    [InlineData(ListingStatus.Draft, "anonymousModerator", false)]
    public async Task AccessDependsOnListingAndViewer(
        ListingStatus status,
        string viewer,
        bool expectedAccess)
    {
        SeedData data = await SeedAsync(status);

        Guid? viewerId = viewer switch
        {
            "owner" => data.OwnerId,
            "moderator" or "other" => Guid.NewGuid(),
            _ => null
        };

        bool canModerate =
            viewer is "moderator" or "anonymousModerator";

        await using RentoXDbContext db = database.CreateContext();
        StorageSpy storage = new();
        ListingImageManagementService service =
            new(db, storage, new TestClock());

        ListingImageContentResult? result =
            viewer == "anonymous"
                ? await service.OpenAsync(data.ImageId)
                : await service.OpenAsync(
                    data.ImageId,
                    viewerId,
                    canModerate);

        using Stream? content = result?.Content;

        if (expectedAccess)
        {
            Assert.NotNull(result);
            Assert.Equal("image/png", result.ContentType);
            Assert.Equal(1, storage.OpenCount);
            Assert.Equal(data.StorageKey, storage.LastOpenedKey);
        }
        else
        {
            Assert.Null(result);
            Assert.Equal(0, storage.OpenCount);
            Assert.Null(storage.LastOpenedKey);
        }
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("expiryBoundary")]
    [InlineData("noExpiry")]
    [InlineData("futurePublication")]
    [InlineData("noPublication")]
    public async Task InvalidPublicationWindowDeniesPublicAccess(
        string scenario)
    {
        SeedData data = await SeedAsync(ListingStatus.Active);

        await using (RentoXDbContext setup = database.CreateContext())
        {
            Listing listing = await setup.Listings.SingleAsync(
                item => item.Id == data.ListingId);

            switch (scenario)
            {
                case "expired":
                    setup.Entry(listing)
                        .Property(item => item.ExpiresAtUtc)
                        .CurrentValue = TestTime.AddSeconds(-1);
                    break;

                case "expiryBoundary":
                    setup.Entry(listing)
                        .Property(item => item.ExpiresAtUtc)
                        .CurrentValue = TestTime;
                    break;

                case "noExpiry":
                    setup.Entry(listing)
                        .Property(item => item.ExpiresAtUtc)
                        .CurrentValue = null;
                    break;

                case "futurePublication":
                    setup.Entry(listing)
                        .Property(item => item.PublishedAtUtc)
                        .CurrentValue = TestTime.AddSeconds(1);
                    break;

                case "noPublication":
                    setup.Entry(listing)
                        .Property(item => item.PublishedAtUtc)
                        .CurrentValue = null;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(scenario));
            }

            await setup.SaveChangesAsync();
        }

        await using RentoXDbContext db = database.CreateContext();
        StorageSpy storage = new();
        ListingImageManagementService service =
            new(db, storage, new TestClock());

        ListingImageContentResult? result =
            await service.OpenAsync(data.ImageId);

        using Stream? content = result?.Content;

        Assert.Null(result);
        Assert.Equal(0, storage.OpenCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SoftDeletedListingDeniesOwnerAndModerator(
        bool useModerator)
    {
        SeedData data = await SeedAsync(ListingStatus.Active);

        await using (RentoXDbContext setup = database.CreateContext())
        {
            Listing listing = await setup.Listings.SingleAsync(
                item => item.Id == data.ListingId);

            listing.MarkAsDeleted(TestTime);
            await setup.SaveChangesAsync();
        }

        await using RentoXDbContext db = database.CreateContext();
        StorageSpy storage = new();
        ListingImageManagementService service =
            new(db, storage, new TestClock());

        ListingImageContentResult? result = await service.OpenAsync(
            data.ImageId,
            useModerator ? Guid.NewGuid() : data.OwnerId,
            useModerator);

        using Stream? content = result?.Content;

        Assert.Null(result);
        Assert.Equal(0, storage.OpenCount);
    }

    [Fact]
    public async Task UnknownImageDoesNotOpenStorage()
    {
        await using RentoXDbContext db = database.CreateContext();
        StorageSpy storage = new();
        ListingImageManagementService service =
            new(db, storage, new TestClock());

        ListingImageContentResult? result =
            await service.OpenAsync(Guid.NewGuid());

        using Stream? content = result?.Content;

        Assert.Null(result);
        Assert.Equal(0, storage.OpenCount);
    }

    [Fact]
    public async Task MissingPhysicalFileReturnsNull()
    {
        SeedData data = await SeedAsync(ListingStatus.Active);

        await using RentoXDbContext db = database.CreateContext();
        StorageSpy storage = new()
        {
            FileExists = false
        };

        ListingImageManagementService service =
            new(db, storage, new TestClock());

        ListingImageContentResult? result =
            await service.OpenAsync(data.ImageId);

        using Stream? content = result?.Content;

        Assert.Null(result);
        Assert.Equal(1, storage.OpenCount);
        Assert.Equal(data.StorageKey, storage.LastOpenedKey);
    }

    private async Task<SeedData> SeedAsync(ListingStatus status)
    {
        await using RentoXDbContext db = database.CreateContext();

        Guid ownerId = Guid.NewGuid();
        string userName = $"listing-image-test-{ownerId:N}";

        AppUser owner = new()
        {
            Id = ownerId,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            RegisteredAtUtc = TestTime,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };

        Category category = Category.Create(
            null,
            $"listing-image-{Guid.NewGuid():N}",
            null,
            0);

        Listing listing = Listing.Create(
            ownerId,
            category.Id,
            "Listing image access test",
            "A listing used by isolated image access tests.",
            100m,
            "AZN",
            (RentalPeriodUnit)2);

        listing.MarkAsCreated(TestTime.AddDays(-1), ownerId);

        string storageKey =
            $"listing-images/{Guid.NewGuid():N}.png";

        ListingImage image = listing.AddImage(
            storageKey,
            "image/png",
            1);

        db.Users.Add(owner);
        db.Categories.Add(category);
        db.Listings.Add(listing);

        // Seed the desired persisted state.
        // These tests cover image access, not lifecycle transitions.
        db.Entry(listing)
            .Property(item => item.Status)
            .CurrentValue = status;

        db.Entry(listing)
            .Property(item => item.PublishedAtUtc)
            .CurrentValue = TestTime.AddDays(-1);

        db.Entry(listing)
            .Property(item => item.ExpiresAtUtc)
            .CurrentValue = TestTime.AddDays(1);

        await db.SaveChangesAsync();

        return new SeedData(
            listing.Id,
            ownerId,
            image.Id,
            storageKey);
    }

    private sealed record SeedData(
        Guid ListingId,
        Guid OwnerId,
        Guid ImageId,
        string StorageKey);

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => TestTime;
    }

    private sealed class StorageSpy : IFileStorage
    {
        public int OpenCount { get; private set; }

        public string? LastOpenedKey { get; private set; }

        public bool FileExists { get; set; } = true;

        public Task<Stream?> OpenReadAsync(
            string storageKey,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            OpenCount++;
            LastOpenedKey = storageKey;

            Stream? content = FileExists
                ? new MemoryStream()
                : null;

            return Task.FromResult(content);
        }

        public Task<StoredFileResult> SaveAsync(
            FileStorageArea area,
            Stream content,
            string contentType,
            string extension,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Image access must not save files.");
        }

        public Task DeleteAsync(
            string storageKey,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Image access must not delete files.");
        }
    }
}
