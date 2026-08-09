namespace RentoX.Contracts.Catalog.Fields;

public sealed record UpdateCategoryFieldRequest(
    string Key,
    int Type,
    bool IsRequired,
    bool IsFilterable,
    bool IsSearchable,
    bool AllowCustomValue,
    bool AppliesToDescendants,
    int DisplayOrder,
    IReadOnlyList<FieldTranslationRequest> Translations);