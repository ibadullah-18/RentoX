namespace RentoX.Contracts.Catalog.Categories;

public sealed record CategoryTranslationRequest(
    int Language,
    string Name);