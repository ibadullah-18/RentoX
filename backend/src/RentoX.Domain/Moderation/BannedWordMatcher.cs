using System.Text.RegularExpressions;

namespace RentoX.Domain.Moderation;

/// <summary>Finds which banned words a text contains. Whole words only, so "sik" is not found in "musiqi".</summary>
public static class BannedWordMatcher
{
    private static readonly TimeSpan Limit = TimeSpan.FromMilliseconds(200);

    /// <param name="words">Banned words as stored (lower case, an optional trailing "*").</param>
    /// <returns>The words that appear in the text, in the order given.</returns>
    public static IReadOnlyList<string> Find(IEnumerable<string> words, params string?[] texts)
    {
        ArgumentNullException.ThrowIfNull(words);

        string haystack = string.Join('\n', texts.Where(t => !string.IsNullOrWhiteSpace(t)))
            .ToLower(BannedWord.Culture);
        if (haystack.Length == 0)
        {
            return [];
        }

        List<string> found = [];
        foreach (string word in words)
        {
            if (Contains(haystack, word))
            {
                found.Add(word);
            }
        }

        return found;
    }

    private static bool Contains(string haystack, string word)
    {
        bool prefix = word.EndsWith('*');
        string core = prefix ? word[..^1] : word;
        if (core.Length == 0)
        {
            return false;
        }

        // Spaces in a phrase match any run of white space.
        string body = string.Join(@"\s+", core.Split(' ').Select(Regex.Escape));
        string pattern = prefix
            ? $@"(?<![\p{{L}}\p{{N}}]){body}"
            : $@"(?<![\p{{L}}\p{{N}}]){body}(?![\p{{L}}\p{{N}}])";

        try
        {
            return Regex.IsMatch(haystack, pattern, RegexOptions.CultureInvariant, Limit);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}
