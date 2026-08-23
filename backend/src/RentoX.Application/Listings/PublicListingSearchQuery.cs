namespace RentoX.Application.Listings;

public sealed record PublicListingSearchQuery(
    Guid? CategoryId,
    string? Search,
    int Page,
    int PageSize,
    Guid? OwnerId = null);