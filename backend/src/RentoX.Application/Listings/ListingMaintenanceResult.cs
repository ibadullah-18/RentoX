namespace RentoX.Application.Listings;

public sealed record ListingMaintenanceResult(
    int ExpiredListingCount,
    int PurgedImageCount,
    int FailedImageCount);