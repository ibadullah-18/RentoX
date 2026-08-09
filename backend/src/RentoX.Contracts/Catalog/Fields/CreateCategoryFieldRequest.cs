namespace RentoX.Contracts.Catalog.Fields;

public sealed record CreateCategoryFieldRequest(
    string Key,
    int Type,
    bool IsRequired,
    bool IsFilterable,
    bool IsSearchable,
    bool AllowCustomValue,
    bool AppliesToDescendants,
    int DisplayOrder,
    IReadOnlyList<FieldTranslationRequest> Translations,
    IReadOnlyList<FieldOptionRequest> Options);