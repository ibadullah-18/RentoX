namespace RentoX.Application.Listings;

public sealed record ListingFieldSelectionValueResult(
    Guid OptionId,
    string Value,
    string Label);