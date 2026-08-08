namespace RentoX.Contracts.Catalog.Categories;

public sealed record UpdateCategoryRequest(
    Guid? ParentId,
    string Slug,
    string? IconUrl,
    int DisplayOrder,
    IReadOnlyList<CategoryTranslationRequest> Translations);