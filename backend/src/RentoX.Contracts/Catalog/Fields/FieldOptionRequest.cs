namespace RentoX.Contracts.Catalog.Fields;

public sealed record FieldOptionRequest(
    string Value,
    int DisplayOrder,
    IReadOnlyList<FieldTranslationRequest> Translations);