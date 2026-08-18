namespace RentoX.Application.Listings;

public sealed record UploadListingImageCommand(
    Guid OwnerId,
    Guid ListingId,
    Stream Content,
    string FileName,
    string ContentType,
    long SizeBytes);