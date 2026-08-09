namespace RentoX.Application.Catalog.Fields;

public interface ICategoryFieldManagementService
{
    Task<CategoryFieldManagementResult> CreateAsync(
        CreateCategoryFieldCommand command,
        CancellationToken cancellationToken = default);
}