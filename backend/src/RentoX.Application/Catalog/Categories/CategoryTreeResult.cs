namespace RentoX.Application.Catalog.Categories;

public sealed record CategoryTreeResult(
    Guid Id,
    Guid? ParentId,
    string Slug,
    string Name,
    string? IconUrl,
    int DisplayOrder,
    IReadOnlyList<CategoryTreeResult> Children)
{
    /// <summary>Rental periods (1 hour .. 6 negotiable) listings may use.</summary>
    public IReadOnlyList<int> RentalPeriods { get; init; } = [];
}