namespace RentoX.Application.Listings;

public sealed record PublicListingSearchQuery(
    Guid? CategoryId,
    string? Search,
    int Page,
    int PageSize,
    Guid? OwnerId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    Guid? OptionId = null,
    Guid[]? OptionIds = null,
    Guid? NumericFieldId = null,
    decimal? NumericMin = null,
    decimal? NumericMax = null,
    Guid? BooleanFieldId = null,
    bool? BooleanValue = null,
    Guid? DateFieldId = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null);