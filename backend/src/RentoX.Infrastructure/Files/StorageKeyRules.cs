using RentoX.Application.Files;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Infrastructure.Files;

/// <summary>
/// What a storage key looks like, the same wherever the files are kept (a folder on the disk or Cloudflare R2):
/// "folder/32 hex characters.extension". The checks stop anything that could point outside the storage.
/// </summary>
internal static class StorageKeyRules
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public static string NewKey(FileStorageArea area, string extension) =>
        $"{FolderOf(area)}/{Guid.NewGuid():N}{NormalizeExtension(extension)}";

    public static string FolderOf(FileStorageArea area)
    {
        return area switch
        {
            FileStorageArea.ListingImages => "listing-images",
            FileStorageArea.StoreLogos => "store-logos",
            FileStorageArea.StoreCovers => "store-covers",
            FileStorageArea.ProfileImages => "profile-images",
            FileStorageArea.ChatAttachments => "chat-attachments",
            FileStorageArea.Banners => "banners",
            _ => throw new DomainException("Storage area is invalid.")
        };
    }

    public static string NormalizeExtension(string extension)
    {
        string normalized = extension.Trim().ToLowerInvariant();

        if (!normalized.StartsWith('.'))
        {
            normalized = $".{normalized}";
        }

        if (!AllowedExtensions.Contains(normalized, StringComparer.Ordinal))
        {
            throw new DomainException("Image extension is not supported.");
        }

        return normalized;
    }

    /// <summary>Throws when the key is empty, rooted, or has a part that is not a plain file or folder name.</summary>
    public static void RequireValidKey(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) ||
            Path.IsPathRooted(storageKey) ||
            storageKey.Contains('\\') ||
            storageKey.Contains(':') ||
            storageKey.Contains('\0'))
        {
            throw new DomainException("Storage key is invalid.");
        }

        foreach (string segment in storageKey.Split('/'))
        {
            if (segment.Length == 0 ||
                segment is "." or ".." ||
                segment.EndsWith('.') ||
                segment.EndsWith(' '))
            {
                throw new DomainException("Storage key is invalid.");
            }

            foreach (char character in segment)
            {
                if (char.IsControl(character) ||
                    character is '<' or '>' or '"' or '|' or '?' or '*')
                {
                    throw new DomainException("Storage key is invalid.");
                }
            }
        }
    }
}
