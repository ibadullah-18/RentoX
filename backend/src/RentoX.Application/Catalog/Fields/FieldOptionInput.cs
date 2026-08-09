namespace RentoX.Application.Catalog.Fields;

public sealed record FieldOptionInput(
    string Value,
    int DisplayOrder,
    IReadOnlyList<FieldTranslationInput> Translations);