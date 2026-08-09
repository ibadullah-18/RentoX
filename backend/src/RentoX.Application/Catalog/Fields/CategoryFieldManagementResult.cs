namespace RentoX.Application.Catalog.Fields;

public sealed record CategoryFieldManagementResult(
    Guid Id,
    Guid CategoryId,
    string Key,
    int Type,
    bool IsRequired,
    bool IsFilterable,
    bool IsSearchable,
    bool AllowCustomValue,
    bool AppliesToDescendants,
    int DisplayOrder,
    bool IsActive,
    IReadOnlyList<CategoryFieldOptionResult> Options);