namespace RentoX.Contracts.Catalog.Fields;

public sealed record CategoryFieldManagementResponse(
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
    IReadOnlyList<CategoryFieldOptionResponse> Options);