namespace RentoX.Application.Catalog.Categories;

public sealed record CategoryTreeResult(
    Guid Id,
    Guid? ParentId,
    string Slug,
    string Name,
    string? IconUrl,
    int DisplayOrder,
    IReadOnlyList<CategoryTreeResult> Children);