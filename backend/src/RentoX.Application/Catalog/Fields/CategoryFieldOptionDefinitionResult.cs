namespace RentoX.Application.Catalog.Fields;

public sealed record CategoryFieldOptionDefinitionResult(
    Guid Id,
    string Value,
    string Label,
    int DisplayOrder);