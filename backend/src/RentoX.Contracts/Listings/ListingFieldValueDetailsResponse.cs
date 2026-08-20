namespace RentoX.Contracts.Listings;

public sealed record ListingFieldValueDetailsResponse(
    Guid FieldId,
    string Key,
    string Label,
    int Type,
    string? TextValue,
    decimal? NumericValue,
    bool? FlagValue,
    DateOnly? CalendarValue,
    string? CustomValue,
    IReadOnlyList<ListingFieldSelectionValueResponse>
        Selections);