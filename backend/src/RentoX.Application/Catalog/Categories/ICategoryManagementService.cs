namespace RentoX.Application.Catalog.Categories;

public interface ICategoryManagementService
{
    Task<CreateCategoryResult> CreateAsync(
        CreateCategoryCommand command,
        CancellationToken cancellationToken = default);

    Task<CreateCategoryResult> UpdateAsync(
        Guid categoryId,
        UpdateCategoryCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Which rental periods listings of this category may use (empty = the
    /// parent's choice, or all of them).
    /// </summary>
    Task SetRentalPeriodsAsync(
        Guid categoryId,
        IReadOnlyCollection<int> periods,
        CancellationToken cancellationToken = default);

    Task SetActiveStatusAsync(
        Guid categoryId,
        bool isActive,
        CancellationToken cancellationToken = default);
}