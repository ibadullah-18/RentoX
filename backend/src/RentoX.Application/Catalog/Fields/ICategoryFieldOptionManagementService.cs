namespace RentoX.Application.Catalog.Fields;

public interface ICategoryFieldOptionManagementService
{
    Task<CategoryFieldOptionResult> CreateAsync(
        Guid categoryId,
        Guid fieldId,
        SaveCategoryFieldOptionCommand command,
        CancellationToken cancellationToken = default);

    Task<CategoryFieldOptionResult> UpdateAsync(
        Guid categoryId,
        Guid fieldId,
        Guid optionId,
        SaveCategoryFieldOptionCommand command,
        CancellationToken cancellationToken = default);

    Task SetActiveStatusAsync(
        Guid categoryId,
        Guid fieldId,
        Guid optionId,
        bool isActive,
        CancellationToken cancellationToken = default);
}