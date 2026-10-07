using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Accounts;
using RentoX.Application.Files;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Files;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Accounts;

public sealed partial class ProfileImageService(
    RentoXDbContext dbContext,
    IFileStorage fileStorage,
    IClock clock,
    ILogger<ProfileImageService> logger)
    : IProfileImageService
{
    private const long MaximumSizeBytes = 5 * 1024 * 1024;

    public async Task<ProfileImageResult> UploadAsync(
        UploadProfileImageCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Content);

        EnsureUserId(command.UserId);

        if (command.SizeBytes is <= 0 or > MaximumSizeBytes)
        {
            throw new DomainException(
                "Profile image must be between 1 byte and 5 MB.");
        }

        if (string.IsNullOrWhiteSpace(command.FileName) ||
            string.IsNullOrWhiteSpace(command.ContentType))
        {
            throw new DomainException(
                "Image file name and content type are required.");
        }

        string extension =
            Path.GetExtension(command.FileName).ToLowerInvariant();

        string contentType =
            command.ContentType.Trim().ToLowerInvariant();

        string? newStorageKey = null;
        string? previousStorageKey = null;
        bool commitAttempted = false;

        await using (var transaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken))
        {
            try
            {
                UserProfile profile =
                    await GetLockedActiveProfileAsync(
                        command.UserId,
                        cancellationToken);

                using MemoryStream validatedContent =
                    await ImageUploadValidator.ReadValidatedAsync(
                        command.Content,
                        command.SizeBytes,
                        contentType,
                        extension,
                        cancellationToken);

                StoredFileResult storedFile =
                    await fileStorage.SaveAsync(
                        FileStorageArea.ProfileImages,
                        validatedContent,
                        contentType,
                        extension,
                        cancellationToken);

                newStorageKey = storedFile.StorageKey;
                previousStorageKey = profile.ProfileImageKey;

                profile.SetProfileImage(newStorageKey);
                profile.MarkAsUpdated(clock.UtcNow, command.UserId);

                await dbContext.SaveChangesAsync(cancellationToken);

                commitAttempted = true;
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                if (!commitAttempted)
                {
                    await transaction.RollbackAsync(
                        CancellationToken.None);

                    if (newStorageKey is not null)
                    {
                        await TryDeleteFileAsync(newStorageKey);
                    }
                }

                // A failed commit response may have an unknown outcome.
                // Keep the new file when commit was attempted.
                throw;
            }
        }

        if (previousStorageKey is not null &&
            !string.Equals(
                previousStorageKey,
                newStorageKey,
                StringComparison.Ordinal))
        {
            await TryDeleteFileAsync(previousStorageKey);
        }

        return new ProfileImageResult(
            command.UserId,
            $"/api/users/{command.UserId}/profile-image");
    }

    public async Task<ProfileImageContentResult?> OpenAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (userId == Guid.Empty)
        {
            return null;
        }

        string? storageKey =
            await dbContext.Set<UserProfile>()
                .AsNoTracking()
                .Where(profile =>
                    profile.Id == userId &&
                    profile.Status == UserStatus.Active &&
                    profile.DeletedAtUtc == null)
                .Select(profile => profile.ProfileImageKey)
                .SingleOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return null;
        }

        string? contentType =
            Path.GetExtension(storageKey).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => null
            };

        if (contentType is null)
        {
            return null;
        }

        Stream? content =
            await fileStorage.OpenReadAsync(
                storageKey,
                cancellationToken);

        return content is null
            ? null
            : new ProfileImageContentResult(content, contentType);
    }

    public async Task DeleteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        EnsureUserId(userId);

        string? previousStorageKey;

        await using (var transaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken))
        {
            UserProfile profile =
                await GetLockedActiveProfileAsync(
                    userId,
                    cancellationToken);

            previousStorageKey = profile.ProfileImageKey;

            if (previousStorageKey is null)
            {
                return;
            }

            profile.SetProfileImage(null);
            profile.MarkAsUpdated(clock.UtcNow, userId);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        await TryDeleteFileAsync(previousStorageKey);
    }

    private async Task<UserProfile> GetLockedActiveProfileAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        // The caller owns a transaction. PostgreSQL holds this row lock
        // until that transaction ends.
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM users.user_profiles WHERE \"Id\" = {userId} FOR UPDATE",
            cancellationToken);

        return await dbContext.Set<UserProfile>()
            .SingleOrDefaultAsync(
                profile =>
                    profile.Id == userId &&
                    profile.Status == UserStatus.Active &&
                    profile.DeletedAtUtc == null,
                cancellationToken)
            ?? throw new DomainException(
                "Active user profile was not found.");
    }

    private async Task TryDeleteFileAsync(string storageKey)
    {
        using CancellationTokenSource timeout =
            new(TimeSpan.FromSeconds(10));

        try
        {
            await fileStorage.DeleteAsync(
                storageKey,
                timeout.Token);
        }
        catch (Exception exception)
        {
            CleanupFailed(logger, exception);
        }
    }

    private static void EnsureUserId(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException(
                "User id is required.");
        }
    }

    [LoggerMessage(
        EventId = 4310,
        Level = LogLevel.Warning,
        Message = "Profile image file cleanup failed.")]
    private static partial void CleanupFailed(
        ILogger logger,
        Exception exception);
}