namespace RentoX.Contracts.Catalog.Categories;

public sealed record CreateCategoryRequest(
    Guid? ParentId,
    string Slug,
    string? IconUrl,
    int DisplayOrder,
    IReadOnlyList<CategoryTranslationRequest> Translations);