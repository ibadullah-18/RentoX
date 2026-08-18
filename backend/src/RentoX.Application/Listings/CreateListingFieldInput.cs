namespace RentoX.Application.Listings;

public sealed record CreateListingFieldInput(
    Guid FieldId,
    string? TextValue,
    decimal? NumericValue,
    bool? FlagValue,
    DateOnly? CalendarValue,
    string? CustomValue,
    IReadOnlyList<Guid> OptionIds);