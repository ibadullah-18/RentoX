using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Catalog.Categories;

public sealed class Category : AggregateRoot
{
    private readonly List<CategoryTranslation> _translations = [];

    private Category()
    {
    }

    private Category(
        Guid id,
        Guid? parentId,
        string slug,
        string? iconUrl,
        int displayOrder)
        : base(id)
    {
        ParentId = parentId;
        Slug = NormalizeSlug(slug);
        IconUrl = NormalizeOptional(iconUrl);
        DisplayOrder = displayOrder;
        IsActive = true;
    }

    public Guid? ParentId { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    public string? IconUrl { get; private set; }

    public int DisplayOrder { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<CategoryTranslation> Translations =>
        _translations.AsReadOnly();

    public static Category Create(
        Guid? parentId,
        string slug,
        string? iconUrl,
        int displayOrder)
    {
        if (displayOrder < 0)
        {
            throw new DomainException(
                "Display order cannot be negative.");
        }

        return new Category(
            Guid.NewGuid(),
            parentId,
            slug,
            iconUrl,
            displayOrder);
    }

    public void AddTranslation(
        CategoryTranslation translation)
    {
        ArgumentNullException.ThrowIfNull(translation);

        if (translation.CategoryId != Id)
        {
            throw new DomainException(
                "Translation does not belong to this category.");
        }

        if (_translations.Any(item =>
                item.Language == translation.Language))
        {
            throw new DomainException(
                "Translation language already exists.");
        }

        _translations.Add(translation);
    }

    public void Update(
    Guid? parentId,
    string slug,
    string? iconUrl,
    int displayOrder)
    {
        if (parentId == Id)
        {
            throw new DomainException(
                "Category cannot be its own parent.");
        }

        if (displayOrder < 0)
        {
            throw new DomainException(
                "Display order cannot be negative.");
        }

        ParentId = parentId;
        Slug = NormalizeSlug(slug);
        IconUrl = NormalizeOptional(iconUrl);
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

    private static string NormalizeSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new DomainException(
                "Category slug is required.");
        }

        return slug.Trim().ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}