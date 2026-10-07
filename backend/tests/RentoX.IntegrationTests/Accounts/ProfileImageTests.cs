using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Accounts;
using RentoX.Application.Files;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Accounts;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Persistence;
using RentoX.IntegrationTests.Auditing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace RentoX.IntegrationTests.Accounts;

public sealed class ProfileImageTests(
    AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    [Theory]
    [InlineData(UserStatus.Active, false, true)]
    [InlineData(UserStatus.Suspended, false, false)]
    [InlineData(UserStatus.Blocked, false, false)]
    [InlineData(UserStatus.Deleted, false, false)]
    [InlineData(UserStatus.Active, true, false)]
    public async Task OnlyActiveProfilesExposeImages(
        UserStatus status,
        bool softDeleted,
        bool expected)
    {
        await using RentoXDbContext context = database.CreateContext();

        UserProfile profile =
            await SeedProfileAsync(context, status, softDeleted, true);

        StorageSpy storage = new();
        ProfileImageService service = CreateService(context, storage);

        ProfileImageContentResult? result =
            await service.OpenAsync(profile.Id);

        using Stream? content = result?.Content;

        Assert.Equal(expected, result is not null);
        Assert.Equal(expected ? 1 : 0, storage.OpenCount);

        if (expected)
        {
            Assert.Equal("image/png", result!.ContentType);
            Assert.Equal(profile.ProfileImageKey, storage.LastOpenedKey);
        }
    }

    [Theory]
    [InlineData(UserStatus.Suspended, false)]
    [InlineData(UserStatus.Blocked, false)]
    [InlineData(UserStatus.Deleted, false)]
    [InlineData(UserStatus.Active, true)]
    public async Task InactiveProfilesCannotChangeImages(
        UserStatus status,
        bool softDeleted)
    {
        await using RentoXDbContext context = database.CreateContext();

        UserProfile profile =
            await SeedProfileAsync(context, status, softDeleted, true);

        StorageSpy storage = new();
        ProfileImageService service = CreateService(context, storage);

        using MemoryStream image = await CreatePngAsync();

        await Assert.ThrowsAsync<DomainException>(
            () => service.UploadAsync(
                CreateCommand(profile.Id, image)));

        await Assert.ThrowsAsync<DomainException>(
            () => service.DeleteAsync(profile.Id));

        Assert.Equal(0, storage.SaveCount);
        Assert.Empty(storage.DeletedKeys);
    }

    [Fact]
    public async Task UploadReplaceAndDeletePersistTheExpectedImage()
    {
        await using RentoXDbContext context = database.CreateContext();

        UserProfile profile =
            await SeedProfileAsync(context, UserStatus.Active, false, false);

        StorageSpy storage = new();
        ProfileImageService service = CreateService(context, storage);

        using MemoryStream first = await CreatePngAsync();

        ProfileImageResult result = await service.UploadAsync(
            CreateCommand(profile.Id, first));

        string firstKey = Assert.IsType<string>(storage.LastSavedKey);

        Assert.Equal(profile.Id, result.UserId);
        Assert.Equal(
            $"/api/users/{profile.Id}/profile-image",
            result.Url);
        Assert.Equal(FileStorageArea.ProfileImages, storage.LastArea);
        Assert.Equal(first.Length, storage.LastSavedLength);
        Assert.Empty(storage.DeletedKeys);

        context.ChangeTracker.Clear();

        string? persistedFirst = await context.Set<UserProfile>()
            .Where(item => item.Id == profile.Id)
            .Select(item => item.ProfileImageKey)
            .SingleAsync();

        Assert.Equal(firstKey, persistedFirst);

        using MemoryStream second = await CreatePngAsync();

        await service.UploadAsync(CreateCommand(profile.Id, second));

        string secondKey = Assert.IsType<string>(storage.LastSavedKey);

        Assert.NotEqual(firstKey, secondKey);
        Assert.Equal(firstKey, Assert.Single(storage.DeletedKeys));

        await service.DeleteAsync(profile.Id);

        context.ChangeTracker.Clear();

        string? remaining = await context.Set<UserProfile>()
            .Where(item => item.Id == profile.Id)
            .Select(item => item.ProfileImageKey)
            .SingleAsync();

        Assert.Null(remaining);
        Assert.Contains(secondKey, storage.DeletedKeys);
        Assert.Equal(2, storage.DeletedKeys.Count);

        await service.DeleteAsync(profile.Id);

        Assert.Equal(2, storage.DeletedKeys.Count);

        ProfileImageContentResult? missing =
            await service.OpenAsync(profile.Id);

        using Stream? missingContent = missing?.Content;

        Assert.Null(missing);
        Assert.Equal(0, storage.OpenCount);
    }

    [Fact]
    public async Task FakeImageDoesNotReplaceExistingImage()
    {
        await using RentoXDbContext context = database.CreateContext();

        UserProfile profile =
            await SeedProfileAsync(context, UserStatus.Active, false, true);

        string? previousKey = profile.ProfileImageKey;

        StorageSpy storage = new();
        ProfileImageService service = CreateService(context, storage);

        byte[] bytes = Encoding.UTF8.GetBytes("Not a PNG image.");
        using MemoryStream content = new(bytes);

        await Assert.ThrowsAsync<DomainException>(
            () => service.UploadAsync(
                CreateCommand(profile.Id, content)));

        context.ChangeTracker.Clear();

        string? currentKey = await context.Set<UserProfile>()
            .Where(item => item.Id == profile.Id)
            .Select(item => item.ProfileImageKey)
            .SingleAsync();

        Assert.Equal(previousKey, currentKey);
        Assert.Equal(0, storage.SaveCount);
        Assert.Empty(storage.DeletedKeys);
    }

    [Fact]
    public async Task UnknownProfileDoesNotOpenStorage()
    {
        await using RentoXDbContext context = database.CreateContext();

        StorageSpy storage = new();
        ProfileImageService service = CreateService(context, storage);

        ProfileImageContentResult? result =
            await service.OpenAsync(Guid.NewGuid());

        using Stream? content = result?.Content;

        Assert.Null(result);
        Assert.Equal(0, storage.OpenCount);
    }

    [Fact]
    public async Task OldFileCleanupFailureDoesNotUndoNewImage()
    {
        await using RentoXDbContext context = database.CreateContext();

        UserProfile profile =
            await SeedProfileAsync(context, UserStatus.Active, false, true);

        StorageSpy storage = new() { FailDelete = true };
        ProfileImageService service = CreateService(context, storage);

        using MemoryStream image = await CreatePngAsync();

        await service.UploadAsync(CreateCommand(profile.Id, image));

        context.ChangeTracker.Clear();

        string? currentKey = await context.Set<UserProfile>()
            .Where(item => item.Id == profile.Id)
            .Select(item => item.ProfileImageKey)
            .SingleAsync();

        Assert.Equal(storage.LastSavedKey, currentKey);
        Assert.Equal(1, storage.SaveCount);
    }

    [Fact]
    public async Task MoreThanFiveMegabytesIsRejectedBeforeStorage()
    {
        await using RentoXDbContext context = database.CreateContext();

        StorageSpy storage = new();
        ProfileImageService service = CreateService(context, storage);

        using MemoryStream content = new();

        UploadProfileImageCommand command = new(
            Guid.NewGuid(),
            content,
            "image.png",
            "image/png",
            5 * 1024 * 1024 + 1);

        await Assert.ThrowsAsync<DomainException>(
            () => service.UploadAsync(command));

        Assert.Equal(0, storage.SaveCount);
    }

    private static ProfileImageService CreateService(
        RentoXDbContext context,
        StorageSpy storage)
    {
        return new ProfileImageService(
            context,
            storage,
            new TestClock(),
            NullLogger<ProfileImageService>.Instance);
    }

    private static UploadProfileImageCommand CreateCommand(
        Guid userId,
        MemoryStream image)
    {
        return new UploadProfileImageCommand(
            userId,
            image,
            "profile.png",
            "image/png",
            image.Length);
    }

    private static async Task<MemoryStream> CreatePngAsync()
    {
        MemoryStream content = new();

        try
        {
            using Image<Rgba32> image = new(2, 2);
            await image.SaveAsPngAsync(content);
            content.Position = 0;
            return content;
        }
        catch
        {
            content.Dispose();
            throw;
        }
    }

    private static async Task<UserProfile> SeedProfileAsync(
        RentoXDbContext context,
        UserStatus status,
        bool softDeleted,
        bool withImage)
    {
        Guid userId = Guid.NewGuid();
        string userName = userId.ToString("N");
        DateTimeOffset now = DateTimeOffset.UtcNow;

        context.Users.Add(new AppUser
        {
            Id = userId,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            RegisteredAtUtc = now
        });

        UserProfile profile = UserProfile.Create(
            userId,
            "Profile Image Test",
            PreferredLanguage.Azerbaijani);

        profile.MarkAsCreated(now, userId);
        profile.ChangeStatus(status);

        if (withImage)
        {
            profile.SetProfileImage(
                "profile-images/" + Guid.NewGuid().ToString("N") + ".png");
        }

        if (softDeleted)
        {
            profile.MarkAsDeleted(now);
        }

        context.Set<UserProfile>().Add(profile);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        return profile;
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    private sealed class StorageSpy : IFileStorage
    {
        public int SaveCount { get; private set; }
        public int OpenCount { get; private set; }
        public string? LastSavedKey { get; private set; }
        public string? LastOpenedKey { get; private set; }
        public long LastSavedLength { get; private set; }
        public FileStorageArea LastArea { get; private set; }
        public List<string> DeletedKeys { get; } = [];
        public bool FailDelete { get; init; }

        public async Task<StoredFileResult> SaveAsync(
            FileStorageArea area,
            Stream content,
            string contentType,
            string extension,
            CancellationToken cancellationToken = default)
        {
            SaveCount++;
            LastArea = area;

            using MemoryStream copy = new();
            await content.CopyToAsync(copy, cancellationToken);

            LastSavedLength = copy.Length;

            string key =
                "profile-images/" + Guid.NewGuid().ToString("N") + extension;

            LastSavedKey = key;

            return new StoredFileResult(
                key,
                contentType,
                copy.Length);
        }

        public Task<Stream?> OpenReadAsync(
            string storageKey,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            OpenCount++;
            LastOpenedKey = storageKey;

            return Task.FromResult<Stream?>(new MemoryStream());
        }

        public Task DeleteAsync(
            string storageKey,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (FailDelete)
            {
                throw new IOException("Simulated file cleanup failure.");
            }

            DeletedKeys.Add(storageKey);
            return Task.CompletedTask;
        }
    }
}