using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Users.Enums;

namespace RentoX.Domain.Catalog.Fields;

public sealed class CategoryFieldTranslation : Entity
{
    private CategoryFieldTranslation()
    {
    }

    private CategoryFieldTranslation(
        Guid id,
        Guid categoryFieldId,
        PreferredLanguage language,
        string label)
        : base(id)
    {
        CategoryFieldId = categoryFieldId;
        Language = language;
        Label = NormalizeLabel(label);
    }

    public Guid CategoryFieldId { get; private set; }

    public PreferredLanguage Language { get; private set; }

    public string Label { get; private set; } = string.Empty;

    public static CategoryFieldTranslation Create(
        Guid categoryFieldId,
        PreferredLanguage language,
        string label)
    {
        if (categoryFieldId == Guid.Empty)
        {
            throw new DomainException(
                "Category field id is required.");
        }

        return new CategoryFieldTranslation(
            Guid.NewGuid(),
            categoryFieldId,
            language,
            label);
    }

    public void UpdateLabel(string label)
    {
        Label = NormalizeLabel(label);
    }

    private static string NormalizeLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new DomainException(
                "Field label is required.");
        }

        return label.Trim();
    }
}