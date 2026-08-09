namespace RentoX.Contracts.Catalog.Fields;

public sealed record CategoryFieldDefinitionResponse(
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
    IReadOnlyList<CategoryFieldOptionDefinitionResponse> Options);