namespace RentoX.Contracts.Catalog.Categories;

public sealed record CategoryTreeResponse(
    Guid Id,
    Guid? ParentId,
    string Slug,
    string Name,
    string? IconUrl,
    int DisplayOrder,
    IReadOnlyList<CategoryTreeResponse> Children);