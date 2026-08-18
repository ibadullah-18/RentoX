namespace RentoX.Application.Listings;

public sealed record ListingImageContentResult(
    Stream Content,
    string ContentType);