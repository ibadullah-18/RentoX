namespace RentoX.Contracts.Catalog.Fields;

public sealed record CategoryFieldOptionDefinitionResponse(
    Guid Id,
    string Value,
    string Label,
    int DisplayOrder);