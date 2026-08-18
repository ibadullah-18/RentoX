namespace RentoX.Contracts.Listings;

public sealed record CreateListingFieldRequest(
    Guid FieldId,
    string? TextValue,
    decimal? NumericValue,
    bool? FlagValue,
    DateOnly? CalendarValue,
    string? CustomValue,
    IReadOnlyList<Guid> OptionIds);