using Microsoft.Extensions.Hosting;
using RentoX.Application.Files;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Infrastructure.Files;

public sealed class LocalFileStorage(
    IHostEnvironment environment)
    : IFileStorage
{
    private const string ListingFolder =
        "listing-images";

    public async Task<StoredFileResult> SaveAsync(
        Stream content,
        string contentType,
        string extension,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        string safeExtension =
            NormalizeExtension(extension);

        string storageKey =
            $"{ListingFolder}/{Guid.NewGuid():N}{safeExtension}";

        string fullPath = GetFullPath(storageKey);

        string? directory =
            Path.GetDirectoryName(fullPath);

        if (directory is null)
        {
            throw new InvalidOperationException(
                "Storage directory could not be resolved.");
        }

        Directory.CreateDirectory(directory);

        await using FileStream output =
            new(
                fullPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81_920,
                useAsync: true);

        await content.CopyToAsync(
            output,
            cancellationToken);

        return new StoredFileResult(
            storageKey,
            contentType,
            output.Length);
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
        string rootPath = Path.GetFullPath(
            Path.Combine(
                environment.ContentRootPath,
                "App_Data"));

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