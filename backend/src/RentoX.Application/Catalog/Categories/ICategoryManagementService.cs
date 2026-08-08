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

    Task SetActiveStatusAsync(
        Guid categoryId,
        bool isActive,
        CancellationToken cancellationToken = default);
}