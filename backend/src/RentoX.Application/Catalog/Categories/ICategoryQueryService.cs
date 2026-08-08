using RentoX.Domain.Users.Enums;

namespace RentoX.Application.Catalog.Categories;

public interface ICategoryQueryService
{
    Task<IReadOnlyList<CategoryTreeResult>> GetTreeAsync(
        PreferredLanguage language,
        CancellationToken cancellationToken = default);
}