namespace RentoX.Contracts.Catalog.Fields;

public sealed record FieldTranslationRequest(
    int Language,
    string Label);