using RentoX.Domain.Users.Enums;

namespace RentoX.Application.Listings;

public interface IListingSuggestionService
{
    Task<IReadOnlyList<ListingSuggestionResult>> SuggestAsync(
        string? text,
        PreferredLanguage language,
        CancellationToken cancellationToken = default);
}
