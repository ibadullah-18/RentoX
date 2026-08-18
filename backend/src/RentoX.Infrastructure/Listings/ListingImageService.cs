using Microsoft.EntityFrameworkCore;
using RentoX.Application.Files;
using RentoX.Application.Listings;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Listings;

public sealed class ListingImageService(
    RentoXDbContext dbContext,
    IFileStorage fileStorage)
    : IListingImageService
{
    private const long MaximumFileSizeBytes =
        10 * 1024 * 1024;

    private static readonly HashSet<string>
        AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

    public async Task<ListingImageResult> UploadAsync(
        UploadListingImageCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Content);

        ValidateFile(command);

        Listing listing =
            await dbContext.Listings
                .Include(item => item.Images)
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == command.ListingId &&
                        item.OwnerId == command.OwnerId,
                    cancellationToken)
            ?? throw new DomainException(
                "Listing was not found.");

        if (listing.Images.Count >=
            Listing.MaximumImageCount)
        {
            throw new DomainException(
                "A listing can contain a maximum of 30 images.");
        }

        string extension =
            Path.GetExtension(command.FileName);

        StoredFileResult storedFile =
            await fileStorage.SaveAsync(
                command.Content,
                command.ContentType,
                extension,
                cancellationToken);

        try
        {
            ListingImage image = listing.AddImage(
                storedFile.StorageKey,
                storedFile.ContentType,
                storedFile.SizeBytes);

            await dbContext.SaveChangesAsync(
                cancellationToken);

            return new ListingImageResult(
                image.Id,
                image.ListingId,
                image.StorageKey,
                image.ContentType,
                image.SizeBytes,
                image.DisplayOrder,
                image.IsCover);
        }
        catch
        {
            await fileStorage.DeleteAsync(
                storedFile.StorageKey,
                cancellationToken);

            throw;
        }
    }

    private static void ValidateFile(
        UploadListingImageCommand command)
    {
        if (command.SizeBytes <= 0)
        {
            throw new DomainException(
                "Image file is empty.");
        }

        if (command.SizeBytes >
            MaximumFileSizeBytes)
        {
            throw new DomainException(
                "Image cannot exceed 10 MB.");
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
}