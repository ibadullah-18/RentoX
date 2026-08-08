using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Catalog.Fields;

public sealed class CategoryFieldOption : Entity
{
    private readonly List<CategoryFieldOptionTranslation>
        _translations = [];

    private CategoryFieldOption()
    {
    }

    private CategoryFieldOption(
        Guid id,
        Guid categoryFieldId,
        string value,
        int displayOrder)
        : base(id)
    {
        CategoryFieldId = categoryFieldId;
        Value = NormalizeValue(value);
        DisplayOrder = displayOrder;
        IsActive = true;
    }

    public Guid CategoryFieldId { get; private set; }

    public string Value { get; private set; } = string.Empty;

    public int DisplayOrder { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<CategoryFieldOptionTranslation>
        Translations => _translations.AsReadOnly();

    public static CategoryFieldOption Create(
        Guid categoryFieldId,
        string value,
        int displayOrder)
    {
        if (categoryFieldId == Guid.Empty)
        {
            throw new DomainException(
                "Category field id is required.");
        }

        if (displayOrder < 0)
        {
            throw new DomainException(
                "Display order cannot be negative.");
        }

        return new CategoryFieldOption(
            Guid.NewGuid(),
            categoryFieldId,
            value,
            displayOrder);
    }

    public void AddTranslation(
        CategoryFieldOptionTranslation translation)
    {
        ArgumentNullException.ThrowIfNull(translation);

        if (translation.CategoryFieldOptionId != Id)
        {
            throw new DomainException(
                "Translation does not belong to this option.");
        }

        if (_translations.Any(item =>
                item.Language == translation.Language))
        {
            throw new DomainException(
                "Translation language already exists.");
        }

        _translations.Add(translation);
    }

    private static string NormalizeValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                "Option value is required.");
        }

        return value.Trim().ToLowerInvariant();
    }
}