namespace RentoX.Application.Catalog.Categories;

public sealed record CreateCategoryCommand(
    Guid? ParentId,
    string Slug,
    string? IconUrl,
    int DisplayOrder,
    IReadOnlyList<CategoryTranslationInput> Translations);