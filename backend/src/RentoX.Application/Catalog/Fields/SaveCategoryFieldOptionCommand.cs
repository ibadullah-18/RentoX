namespace RentoX.Application.Catalog.Fields;

public sealed record SaveCategoryFieldOptionCommand(
    string Value,
    int DisplayOrder,
    IReadOnlyList<FieldTranslationInput> Translations);