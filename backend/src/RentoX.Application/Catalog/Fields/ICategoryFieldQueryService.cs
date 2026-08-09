using RentoX.Domain.Users.Enums;

namespace RentoX.Application.Catalog.Fields;

public interface ICategoryFieldQueryService
{
    Task<IReadOnlyList<CategoryFieldDefinitionResult>>
        GetForCategoryAsync(
            Guid categoryId,
            PreferredLanguage language,
            CancellationToken cancellationToken = default);
}