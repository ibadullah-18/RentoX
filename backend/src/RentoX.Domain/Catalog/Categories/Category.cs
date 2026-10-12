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

    /// <summary>
    /// Which rental periods (hour, day, week, month...) listings of this
    /// category may use: bit <c>unit - 1</c> per period. 0 = not set here,
    /// so the parent's choice (or "everything") applies.
    /// </summary>
    public int RentalPeriodMask { get; private set; }

    public const int AllRentalPeriodsMask = 0b111111;

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

    /// <summary>
    /// Sets the allowed rental periods (values 1..6); an empty list means
    /// "inherit".
    /// </summary>
    public void SetRentalPeriods(IEnumerable<int> periods)
    {
        ArgumentNullException.ThrowIfNull(periods);

        int mask = 0;

        foreach (int period in periods)
        {
            if (period is < 1 or > 6)
            {
                throw new DomainException(
                    "Rental period is invalid.");
            }

            mask |= 1 << (period - 1);
        }

        RentalPeriodMask = mask;
    }

    public static IReadOnlyList<int> PeriodsOf(int mask) =>
        [.. Enumerable.Range(1, 6).Where(p => (mask & (1 << (p - 1))) != 0)];

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