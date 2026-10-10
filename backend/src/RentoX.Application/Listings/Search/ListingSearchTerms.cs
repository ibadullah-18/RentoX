using System.Globalization;
using System.Text;

namespace RentoX.Application.Listings.Search;

/// <summary>
/// One word of a search query together with the other words that mean the
/// same thing (synonyms in Azerbaijani, Russian and English, plural forms).
/// The first alternative is always the word the user typed.
/// </summary>
public sealed record SearchToken(
    string Text,
    IReadOnlyList<string> Alternatives);

/// <summary>
/// Turns what the user typed into normalized search words.
/// <para>
/// <see cref="Normalize"/> must stay identical to the database function
/// <c>public.rentox_norm</c> (see the AddSmartSearch migration): both sides of
/// the comparison are normalized the same way, so "Maşın", "masin" and
/// "MAŞIN" are the same word.
/// </para>
/// </summary>
public static class ListingSearchTerms
{
    public const int MaximumTokens = 6;
    public const int MaximumAlternatives = 6;

    private static readonly HashSet<string> StopWords =
        new(StringComparer.Ordinal)
        {
            "ve", "ile", "ucun", "the", "and", "for", "with",
            "и", "в", "на", "для", "с", "по"
        };

    // Groups of words that mean the same thing, already in normalized form
    // (no Azerbaijani special letters, lower case). Add a group here and the
    // search understands it everywhere; no database change is needed.
    private static readonly string[][] SynonymGroups =
    [
        ["masin", "avtomobil", "avto", "car", "auto", "mashin", "машина", "автомобиль", "авто"],
        ["menzil", "ev", "kvartira", "apartment", "flat", "квартира"],
        ["villa", "kottec", "cottage", "house", "дом", "дача", "коттедж"],
        ["motosiklet", "moto", "motocikl", "motorcycle", "motorbike", "мотоцикл", "мото"],
        ["skuter", "scooter", "самокат", "скутер"],
        ["velosiped", "bike", "bicycle", "велосипед"],
        ["texnika", "avadanliq", "equipment", "техника", "оборудование"],
        ["alet", "alat", "tool", "instrument", "инструмент"],
        ["avtobus", "bus", "mikroavtobus", "minivan", "автобус", "микроавтобус"],
        ["yuk", "truck", "kamaz", "gruzovik", "грузовик"],
        ["cadir", "tent", "палатка", "шатер"],
        ["toy", "wedding", "dugun", "свадьба"],
        ["kiraye", "icare", "rent", "rental", "аренда", "прокат"],
        ["telefon", "phone", "mobil", "телефон"],
        ["noutbuk", "laptop", "komputer", "ноутбук"],
        ["kamera", "camera", "фотоаппарат", "камера"],
        ["generator", "agregat", "генератор"]
    ];

    private static readonly string[] PluralSuffixes = ["lari", "leri", "lar", "ler"];

    private static readonly Dictionary<string, string[]> SynonymLookup =
        BuildSynonymLookup();

    /// <summary>
    /// Lower case, Azerbaijani letters folded to Latin (ə→e, ı→i, ö→o, ü→u,
    /// ğ→g, ç→c, ş→s), "ё"→"е", everything that is not a letter or digit
    /// becomes a single space.
    /// </summary>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        StringBuilder builder = new(value.Length);
        bool pendingSpace = false;

        foreach (char raw in value)
        {
            char? mapped = Fold(raw);

            if (mapped is null)
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(mapped.Value);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Splits the query into words (at most <see cref="MaximumTokens"/>) and
    /// lists the alternatives of each word.
    /// </summary>
    public static IReadOnlyList<SearchToken> Parse(string? raw)
    {
        string normalized = Normalize(raw);

        if (normalized.Length == 0)
        {
            return [];
        }

        List<SearchToken> tokens = [];
        HashSet<string> seen = new(StringComparer.Ordinal);

        foreach (string word in normalized.Split(' '))
        {
            if (word.Length == 0 ||
                StopWords.Contains(word) ||
                !seen.Add(word))
            {
                continue;
            }

            tokens.Add(new SearchToken(word, AlternativesOf(word)));

            if (tokens.Count == MaximumTokens)
            {
                break;
            }
        }

        return tokens;
    }

    private static List<string> AlternativesOf(string word)
    {
        List<string> result = [word];

        void Add(string candidate)
        {
            if (candidate.Length >= 2 &&
                result.Count < MaximumAlternatives &&
                !result.Contains(candidate))
            {
                result.Add(candidate);
            }
        }

        // "maşınlar" → "maşın": the singular form is what titles contain.
        string stem = Stem(word);
        Add(stem);

        if (SynonymLookup.TryGetValue(stem, out string[]? group))
        {
            foreach (string synonym in group)
            {
                Add(synonym);
            }
        }

        return result;
    }

    private static string Stem(string word)
    {
        if (word.Length < 5)
        {
            return word;
        }

        foreach (string suffix in PluralSuffixes)
        {
            if (word.Length - suffix.Length >= 3 &&
                word.EndsWith(suffix, StringComparison.Ordinal))
            {
                return word[..^suffix.Length];
            }
        }

        return word;
    }

    private static char? Fold(char c)
    {
        return c switch
        {
            'ə' or 'Ə' => 'e',
            'ı' or 'I' or 'İ' => 'i',
            'ö' or 'Ö' => 'o',
            'ü' or 'Ü' => 'u',
            'ğ' or 'Ğ' => 'g',
            'ç' or 'Ç' => 'c',
            'ş' or 'Ş' => 's',
            'ё' or 'Ё' => 'е',
            _ when char.IsLetterOrDigit(c) =>
                char.ToLower(c, CultureInfo.InvariantCulture),
            _ => null
        };
    }

    private static Dictionary<string, string[]> BuildSynonymLookup()
    {
        Dictionary<string, string[]> lookup = new(StringComparer.Ordinal);

        foreach (string[] group in SynonymGroups)
        {
            foreach (string word in group)
            {
                lookup[Normalize(word)] = group;
            }
        }

        return lookup;
    }
}
