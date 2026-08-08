namespace RentoX.Application.Catalog.Categories;

public sealed record CreateCategoryResult(
    Guid Id,
    Guid? ParentId,
    string Slug,
    string? IconUrl,
    int DisplayOrder,
    bool IsActive);