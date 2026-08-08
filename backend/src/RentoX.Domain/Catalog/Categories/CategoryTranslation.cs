using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Users.Enums;

namespace RentoX.Domain.Catalog.Categories;

public sealed class CategoryTranslation : Entity
{
    private CategoryTranslation()
    {
    }

    private CategoryTranslation(
        Guid id,
        Guid categoryId,
        PreferredLanguage language,
        string name)
        : base(id)
    {
        CategoryId = categoryId;
        Language = language;
        Name = NormalizeName(name);
    }

    public Guid CategoryId { get; private set; }

    public PreferredLanguage Language { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public static CategoryTranslation Create(
        Guid categoryId,
        PreferredLanguage language,
        string name)
    {
        if (categoryId == Guid.Empty)
        {
            throw new DomainException(
                "Category id is required.");
        }

        return new CategoryTranslation(
            Guid.NewGuid(),
            categoryId,
            language,
            name);
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException(
                "Category name is required.");
        }

        return name.Trim();
    }
}