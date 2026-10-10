namespace RentoX.Application.Listings;

/// <summary>
/// One line of the search box drop-down: a word people use in listing titles
/// (or a category name) with the category chain it belongs to.
/// </summary>
public sealed record ListingSuggestionResult(
    string Kind,
    string Text,
    Guid? CategoryId,
    IReadOnlyList<string> CategoryPath);
