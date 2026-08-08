namespace RentoX.Application.Catalog.Categories;

public interface ICategoryManagementService
{
    Task<CreateCategoryResult> CreateAsync(
        CreateCategoryCommand command,
        CancellationToken cancellationToken = default);
}