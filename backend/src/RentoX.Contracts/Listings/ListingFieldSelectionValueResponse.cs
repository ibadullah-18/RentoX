namespace RentoX.Contracts.Listings;

public sealed record ListingFieldSelectionValueResponse(
    Guid OptionId,
    string Value,
    string Label);