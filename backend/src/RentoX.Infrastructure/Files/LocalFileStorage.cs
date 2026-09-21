using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RentoX.Application.Files;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Infrastructure.Files;

public sealed class LocalFileStorage(
    IHostEnvironment environment,
    IConfiguration configuration)
    : IFileStorage
{

    public async Task<StoredFileResult> SaveAsync(
        FileStorageArea area,
        Stream content,
        string contentType,
        string extension,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        string safeExtension =
            NormalizeExtension(extension);

        string folder = GetFolder(area);

        string storageKey =
            $"{folder}/{Guid.NewGuid():N}{safeExtension}";

        string fullPath = GetFullPath(storageKey);

        string? directory =
            Path.GetDirectoryName(fullPath);

        if (directory is null)
        {
            throw new InvalidOperationException(
                "Storage directory could not be resolved.");
        }

        Directory.CreateDirectory(directory);

        bool created = false;
        try
        {
            await using FileStream output = new(
                fullPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81_920,
                useAsync: true);
            created = true;
            await content.CopyToAsync(output, cancellationToken);
            return new StoredFileResult(storageKey, contentType, output.Length);
        }
        catch
        {
            if (created)
            {
                File.Delete(fullPath);
            }
            throw;
        }
    }

    public Task<Stream?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string fullPath = GetFullPath(storageKey);

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81_920,
            useAsync: true);

        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string fullPath = GetFullPath(storageKey);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private string GetFullPath(string storageKey)
    {
        string rootPath =
            ResolveRootPath();

        string fullPath = Path.GetFullPath(
            Path.Combine(
                rootPath,
                storageKey.Replace(
                    '/',
                    Path.DirectorySeparatorChar)));

        string requiredPrefix =
            rootPath.EndsWith(
                Path.DirectorySeparatorChar)
                ? rootPath
                : rootPath +
                  Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(
                requiredPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException(
                "Storage key is invalid.");
        }

        return fullPath;
    }

    private string ResolveRootPath()
    {
        string? configuredPath =
            configuration["Storage:RootPath"];

        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.GetFullPath(
                Path.Combine(
                    environment.ContentRootPath,
                    "App_Data"));
        }

        return Path.GetFullPath(
            Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(
                    environment.ContentRootPath,
                    configuredPath));
    }

    private static string GetFolder(
    FileStorageArea area)
    {
        return area switch
        {
            FileStorageArea.ListingImages =>
                "listing-images",

            FileStorageArea.StoreLogos =>
                "store-logos",

            FileStorageArea.StoreCovers =>
                "store-covers",

            FileStorageArea.ProfileImages =>
                "profile-images",

            FileStorageArea.ChatAttachments =>
                "chat-attachments",

            _ => throw new DomainException(
                "Storage area is invalid.")
        };
    }

    private static string NormalizeExtension(
        string extension)
    {
        string normalized =
            extension.Trim().ToLowerInvariant();

        if (!normalized.StartsWith('.'))
        {
            normalized = $".{normalized}";
        }

        string[] allowedExtensions =
        [
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        ];

        if (!allowedExtensions.Contains(
                normalized,
                StringComparer.Ordinal))
        {
            throw new DomainException(
                "Image extension is not supported.");
        }

        return normalized;
    }
}
