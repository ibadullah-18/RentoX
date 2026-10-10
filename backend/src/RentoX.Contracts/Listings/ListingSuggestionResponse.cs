namespace RentoX.Contracts.Listings;

/// <summary>
/// <c>Kind</c> is "word" (a word from listing titles, found in
/// <c>CategoryId</c>) or "category".
/// </summary>
public sealed record ListingSuggestionResponse(
    string Kind,
    string Text,
    Guid? CategoryId,
    IReadOnlyList<string> CategoryPath);
