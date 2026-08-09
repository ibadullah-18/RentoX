namespace RentoX.Contracts.Catalog.Fields;

public sealed record SaveCategoryFieldOptionRequest(
    string Value,
    int DisplayOrder,
    IReadOnlyList<FieldTranslationRequest> Translations);