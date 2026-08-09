namespace RentoX.Application.Catalog.Fields;

public sealed record CreateCategoryFieldCommand(
    Guid CategoryId,
    string Key,
    int Type,
    bool IsRequired,
    bool IsFilterable,
    bool IsSearchable,
    bool AllowCustomValue,
    bool AppliesToDescendants,
    int DisplayOrder,
    IReadOnlyList<FieldTranslationInput> Translations,
    IReadOnlyList<FieldOptionInput> Options);