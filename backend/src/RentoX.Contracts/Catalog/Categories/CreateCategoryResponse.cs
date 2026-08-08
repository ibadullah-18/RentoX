namespace RentoX.Contracts.Catalog.Categories;

public sealed record CreateCategoryResponse(
    Guid Id,
    Guid? ParentId,
    string Slug,
    string? IconUrl,
    int DisplayOrder,
    bool IsActive);