namespace RentoX.Application.Catalog.Categories;

public sealed record UpdateCategoryCommand(
    Guid? ParentId,
    string Slug,
    string? IconUrl,
    int DisplayOrder,
    IReadOnlyList<CategoryTranslationInput> Translations);