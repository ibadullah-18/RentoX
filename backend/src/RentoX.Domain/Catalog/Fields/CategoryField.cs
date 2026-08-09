using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Catalog.Fields;

public sealed class CategoryField : AggregateRoot
{
    private readonly List<CategoryFieldTranslation> _translations = [];
    private readonly List<CategoryFieldOption> _options = [];

    private CategoryField()
    {
    }

    private CategoryField(
        Guid id,
        Guid categoryId,
        string key,
        CategoryFieldType type,
        bool isRequired,
        bool isFilterable,
        bool isSearchable,
        bool allowCustomValue,
        bool appliesToDescendants,
        int displayOrder)
        : base(id)
    {
        CategoryId = categoryId;
        Key = NormalizeKey(key);
        Type = type;
        IsRequired = isRequired;
        IsFilterable = isFilterable;
        IsSearchable = isSearchable;
        AllowCustomValue = allowCustomValue;
        AppliesToDescendants = appliesToDescendants;
        DisplayOrder = displayOrder;
        IsActive = true;
    }

    public Guid CategoryId { get; private set; }

    public string Key { get; private set; } = string.Empty;

    public CategoryFieldType Type { get; private set; }

    public bool IsRequired { get; private set; }

    public bool IsFilterable { get; private set; }

    public bool IsSearchable { get; private set; }

    public bool AllowCustomValue { get; private set; }

    public bool AppliesToDescendants { get; private set; }

    public int DisplayOrder { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<CategoryFieldTranslation> Translations =>
        _translations.AsReadOnly();

    public IReadOnlyCollection<CategoryFieldOption> Options =>
        _options.AsReadOnly();

    public static CategoryField Create(
        Guid categoryId,
        string key,
        CategoryFieldType type,
        bool isRequired,
        bool isFilterable,
        bool isSearchable,
        bool allowCustomValue,
        bool appliesToDescendants,
        int displayOrder)
    {
        if (categoryId == Guid.Empty)
        {
            throw new DomainException(
                "Category id is required.");
        }

        if (!Enum.IsDefined(type))
        {
            throw new DomainException(
                "Category field type is invalid.");
        }

        if (displayOrder < 0)
        {
            throw new DomainException(
                "Display order cannot be negative.");
        }

        return new CategoryField(
            Guid.NewGuid(),
            categoryId,
            key,
            type,
            isRequired,
            isFilterable,
            isSearchable,
            allowCustomValue,
            appliesToDescendants,
            displayOrder);
    }

    public void AddTranslation(
        CategoryFieldTranslation translation)
    {
        ArgumentNullException.ThrowIfNull(translation);

        if (translation.CategoryFieldId != Id)
        {
            throw new DomainException(
                "Translation does not belong to this field.");
        }

        if (_translations.Any(item =>
                item.Language == translation.Language))
        {
            throw new DomainException(
                "Translation language already exists.");
        }

        _translations.Add(translation);
    }

    public void AddOption(CategoryFieldOption option)
    {
        ArgumentNullException.ThrowIfNull(option);

        if (option.CategoryFieldId != Id)
        {
            throw new DomainException(
                "Option does not belong to this field.");
        }

        _options.Add(option);
    }

    public void Update(
    string key,
    CategoryFieldType type,
    bool isRequired,
    bool isFilterable,
    bool isSearchable,
    bool allowCustomValue,
    bool appliesToDescendants,
    int displayOrder)
    {
        if (!Enum.IsDefined(type))
        {
            throw new DomainException(
                "Category field type is invalid.");
        }

        if (displayOrder < 0)
        {
            throw new DomainException(
                "Display order cannot be negative.");
        }

        Key = NormalizeKey(key);
        Type = type;
        IsRequired = isRequired;
        IsFilterable = isFilterable;
        IsSearchable = isSearchable;
        AllowCustomValue = allowCustomValue;
        AppliesToDescendants = appliesToDescendants;
        DisplayOrder = displayOrder;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private static string NormalizeKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainException(
                "Category field key is required.");
        }

        return key.Trim().ToLowerInvariant();
    }
}