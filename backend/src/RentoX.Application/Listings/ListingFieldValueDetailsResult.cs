namespace RentoX.Application.Listings;

public sealed record ListingFieldValueDetailsResult(
    Guid FieldId,
    string Key,
    string Label,
    int Type,
    string? TextValue,
    decimal? NumericValue,
    bool? FlagValue,
    DateOnly? CalendarValue,
    string? CustomValue,
    IReadOnlyList<ListingFieldSelectionValueResult>
        Selections);