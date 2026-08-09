namespace RentoX.Application.Catalog.Fields;

public sealed record CategoryFieldDefinitionResult(
    Guid Id,
    Guid SourceCategoryId,
    string Key,
    string Label,
    int Type,
    bool IsRequired,
    bool IsFilterable,
    bool IsSearchable,
    bool AllowCustomValue,
    int DisplayOrder,
    IReadOnlyList<CategoryFieldOptionDefinitionResult> Options);