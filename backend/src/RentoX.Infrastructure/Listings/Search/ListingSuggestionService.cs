using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Listings;
using RentoX.Application.Listings.Search;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Listings.Search;

/// <summary>
/// Search-as-you-type suggestions: words from active listing titles that
/// start with what was typed (with the category chain they appear in), then
/// matching category names.
/// </summary>
public sealed class ListingSuggestionService(
    RentoXDbContext dbContext,
    IClock clock)
    : IListingSuggestionService
{
    private const int MaximumWords = 8;
    private const int MaximumCategories = 3;
    private const int MaximumTextLength = 60;

    public async Task<IReadOnlyList<ListingSuggestionResult>> SuggestAsync(
        string? text,
        PreferredLanguage language,
        CancellationToken cancellationToken = default)
    {
        string typed = ListingSearchTerms.Normalize(
            text is { Length: > MaximumTextLength }
                ? text[..MaximumTextLength]
                : text);

        if (typed.Length < 2)
        {
            return [];
        }

        List<CategoryRow> categories = await dbContext.Categories
            .AsNoTracking()
            .Select(category => new CategoryRow
            {
                Id = category.Id,
                ParentId = category.ParentId,
                Names = category.Translations
                    .Select(t => new CategoryName
                    {
                        Language = t.Language,
                        Name = t.Name
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        Dictionary<Guid, CategoryNode> byId = categories.ToDictionary(
            c => c.Id,
            c => new CategoryNode(
                c.ParentId,
                (c.Names.FirstOrDefault(n => n.Language == language)
                    ?? c.Names.FirstOrDefault(n =>
                        n.Language == PreferredLanguage.Azerbaijani)
                    ?? c.Names.FirstOrDefault())?.Name ?? string.Empty,
                c.Names
                    .Select(n => ListingSearchTerms.Normalize(n.Name))
                    .ToList()));

        List<ListingSuggestionResult> result = [];

        foreach (WordRow row in await FindWordsAsync(typed, cancellationToken))
        {
            if (!byId.ContainsKey(row.CategoryId))
            {
                continue;
            }

            result.Add(new ListingSuggestionResult(
                "word",
                row.Word,
                row.CategoryId,
                Chain(byId, row.CategoryId)));
        }

        string inside = " " + typed;

        foreach (CategoryRow category in categories
                     .Where(c => byId[c.Id].Normalized.Any(name =>
                         name.StartsWith(typed, StringComparison.Ordinal) ||
                         (" " + name).Contains(
                             inside,
                             StringComparison.Ordinal)))
                     .Take(MaximumCategories))
        {
            List<string> chain = Chain(byId, category.Id);

            result.Add(new ListingSuggestionResult(
                "category",
                chain[^1],
                category.Id,
                chain.Take(chain.Count - 1).ToList()));
        }

        return result;
    }

    // Names from the top category down to (and including) this one.
    private static List<string> Chain(
        Dictionary<Guid, CategoryNode> byId,
        Guid id)
    {
        List<string> chain = [];
        Guid? current = id;

        for (int i = 0; current.HasValue && i < 10; i++)
        {
            if (!byId.TryGetValue(current.Value, out CategoryNode? node))
            {
                break;
            }

            chain.Insert(0, node.Name);
            current = node.ParentId;
        }

        return chain;
    }

    private async Task<List<WordRow>> FindWordsAsync(
        string typed,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT w.word AS "Word", w.category_id AS "CategoryId", count(*)::bigint AS "Count"
            FROM (
              SELECT lower(x.word) AS word, l."CategoryId" AS category_id
              FROM listings.listings l
              CROSS JOIN LATERAL regexp_split_to_table(l."Title", '[^[:alnum:]]+') AS x(word)
              WHERE l."Status" = @status
                AND l."DeletedAtUtc" IS NULL
                AND l."ExpiresAtUtc" > @now
            ) w
            WHERE length(w.word) >= 2
              AND public.rentox_norm(w.word) LIKE @prefix || '%'
            GROUP BY w.word, w.category_id
            ORDER BY count(*) DESC, w.word
            LIMIT 40
            """;

        try
        {
            List<WordRow> rows = await dbContext.Database
                .SqlQueryRaw<WordRow>(
                    sql,
                    new NpgsqlParameter("status", NpgsqlDbType.Integer)
                    {
                        Value = (int)ListingStatus.Active
                    },
                    new NpgsqlParameter("now", NpgsqlDbType.TimestampTz)
                    {
                        Value = clock.UtcNow.UtcDateTime
                    },
                    new NpgsqlParameter("prefix", NpgsqlDbType.Text)
                    {
                        Value = typed
                    })
                .ToListAsync(cancellationToken);

            return rows.Take(MaximumWords).ToList();
        }
        catch (PostgresException exception)
            when (exception.SqlState == PostgresErrorCodes.UndefinedFunction)
        {
            // Migration not applied yet: only category suggestions.
            return [];
        }
    }

    public sealed class WordRow
    {
        public string Word { get; set; } = string.Empty;

        public Guid CategoryId { get; set; }

        public long Count { get; set; }
    }

    private sealed class CategoryName
    {
        public PreferredLanguage Language { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private sealed class CategoryRow
    {
        public Guid Id { get; set; }

        public Guid? ParentId { get; set; }

        public List<CategoryName> Names { get; set; } = [];
    }

    private sealed record CategoryNode(
        Guid? ParentId,
        string Name,
        List<string> Normalized);
}
