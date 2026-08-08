using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Users.Enums;

namespace RentoX.Domain.Catalog.Fields;

public sealed class CategoryFieldOptionTranslation : Entity
{
    private CategoryFieldOptionTranslation()
    {
    }

    private CategoryFieldOptionTranslation(
        Guid id,
        Guid categoryFieldOptionId,
        PreferredLanguage language,
        string label)
        : base(id)
    {
        CategoryFieldOptionId = categoryFieldOptionId;
        Language = language;
        Label = NormalizeLabel(label);
    }

    public Guid CategoryFieldOptionId { get; private set; }

    public PreferredLanguage Language { get; private set; }

    public string Label { get; private set; } = string.Empty;

    public static CategoryFieldOptionTranslation Create(
        Guid categoryFieldOptionId,
        PreferredLanguage language,
        string label)
    {
        if (categoryFieldOptionId == Guid.Empty)
        {
            throw new DomainException(
                "Category field option id is required.");
        }

        return new CategoryFieldOptionTranslation(
            Guid.NewGuid(),
            categoryFieldOptionId,
            language,
            label);
    }

    private static string NormalizeLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new DomainException(
                "Option label is required.");
        }

        return label.Trim();
    }
}