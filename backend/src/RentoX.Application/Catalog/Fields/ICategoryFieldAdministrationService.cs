namespace RentoX.Application.Catalog.Fields;

public interface ICategoryFieldAdministrationService
{
    Task<CategoryFieldManagementResult> UpdateAsync(
        Guid fieldId,
        UpdateCategoryFieldCommand command,
        CancellationToken cancellationToken = default);

    Task SetActiveStatusAsync(
        Guid fieldId,
        bool isActive,
        CancellationToken cancellationToken = default);
}