namespace RentoX.Contracts.Listings;

/// <summary>One element of the <c>filters</c> JSON array of the listing search.</summary>
public sealed record ListingFieldFilterRequest(
    Guid FieldId,
    Guid[]? OptionIds = null,
    decimal? Min = null,
    decimal? Max = null,
    bool? Flag = null,
    DateOnly? From = null,
    DateOnly? To = null);
