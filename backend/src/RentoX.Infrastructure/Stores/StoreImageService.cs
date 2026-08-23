using Microsoft.EntityFrameworkCore;
using RentoX.Application.Files;
using RentoX.Application.Stores;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Stores;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Stores;

public sealed class StoreImageService(
    RentoXDbContext dbContext,
    IFileStorage fileStorage)
    : IStoreImageService
{
    private const long MaximumLogoSize =
        5 * 1024 * 1024;

    private const long MaximumCoverSize =
        10 * 1024 * 1024;

    private static readonly HashSet<string>
        AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

    public async Task<StoreImageResult> UploadAsync(
        UploadStoreImageCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Content);

        ValidateFile(command);

        StoreProfile store =
            await dbContext.StoreProfiles
                .SingleOrDefaultAsync(
                    item =>
                        item.OwnerId == command.OwnerId,
                    cancellationToken)
            ?? throw new DomainException(
                "Store was not found.");

        string? previousStorageKey =
            command.Kind == StoreImageKind.Logo
                ? store.LogoImageKey
                : store.CoverImageKey;

        FileStorageArea storageArea =
            command.Kind == StoreImageKind.Logo
                ? FileStorageArea.StoreLogos
                : FileStorageArea.StoreCovers;

        string extension =
            Path.GetExtension(command.FileName);

        StoredFileResult storedFile =
            await fileStorage.SaveAsync(
                storageArea,
                command.Content,
                command.ContentType,
                extension,
                cancellationToken);

        try
        {
            if (command.Kind == StoreImageKind.Logo)
            {
                store.SetLogo(storedFile.StorageKey);
            }
            else
            {
                store.SetCover(storedFile.StorageKey);
            }

            await dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch
        {
            await fileStorage.DeleteAsync(
                storedFile.StorageKey,
                cancellationToken);

            throw;
        }

        if (previousStorageKey is not null)
        {
            await fileStorage.DeleteAsync(
                previousStorageKey,
                cancellationToken);
        }

        return new StoreImageResult(
            store.Id,
            command.Kind);
    }

    public async Task<StoreImageContentResult?> OpenAsync(
        Guid storeId,
        StoreImageKind kind,
        CancellationToken cancellationToken = default)
    {
        StoreProfile? store =
            await dbContext.StoreProfiles
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.Id == storeId,
                    cancellationToken);

        if (store is null)
        {
            return null;
        }

        string? storageKey =
            kind == StoreImageKind.Logo
                ? store.LogoImageKey
                : store.CoverImageKey;

        if (storageKey is null)
        {
            return null;
        }

        Stream? content =
            await fileStorage.OpenReadAsync(
                storageKey,
                cancellationToken);

        if (content is null)
        {
            return null;
        }

        return new StoreImageContentResult(
            content,
            GetContentType(storageKey));
    }

    public async Task DeleteAsync(
        Guid ownerId,
        StoreImageKind kind,
        CancellationToken cancellationToken = default)
    {
        StoreProfile store =
            await dbContext.StoreProfiles
                .SingleOrDefaultAsync(
                    item => item.OwnerId == ownerId,
                    cancellationToken)
            ?? throw new DomainException(
                "Store was not found.");

        string? storageKey =
            kind == StoreImageKind.Logo
                ? store.LogoImageKey
                : store.CoverImageKey;

        if (storageKey is null)
        {
            return;
        }

        if (kind == StoreImageKind.Logo)
        {
            store.SetLogo(null);
        }
        else
        {
            store.SetCover(null);
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await fileStorage.DeleteAsync(
            storageKey,
            cancellationToken);
    }

    private static void ValidateFile(
        UploadStoreImageCommand command)
    {
        if (!Enum.IsDefined(command.Kind))
        {
            throw new DomainException(
                "Store image kind is invalid.");
        }

        if (command.SizeBytes <= 0)
        {
            throw new DomainException(
                "Image file is empty.");
        }

        long maximumSize =
            command.Kind == StoreImageKind.Logo
                ? MaximumLogoSize
                : MaximumCoverSize;

        if (command.SizeBytes > maximumSize)
        {
            throw new DomainException(
                command.Kind == StoreImageKind.Logo
                    ? "Logo cannot exceed 5 MB."
                    : "Cover image cannot exceed 10 MB.");
        }

        if (!AllowedContentTypes.Contains(
                command.ContentType))
        {
            throw new DomainException(
                "Only JPEG, PNG and WebP images are supported.");
        }

        if (string.IsNullOrWhiteSpace(
                command.FileName))
        {
            throw new DomainException(
                "Image file name is required.");
        }
    }

    private static string GetContentType(
        string storageKey)
    {
        string extension =
            Path.GetExtension(storageKey)
                .ToLowerInvariant();

        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}