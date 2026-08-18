namespace RentoX.Application.Listings;

public interface IListingImageService
{
    Task<ListingImageResult> UploadAsync(
        UploadListingImageCommand command,
        CancellationToken cancellationToken = default);
}