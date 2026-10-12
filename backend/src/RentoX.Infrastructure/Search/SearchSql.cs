using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using RentoX.Application.Listings.Search;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Search;

/// <summary>One scored row (id and relevance) returned by a search query.</summary>
public sealed class SearchScore
{
    public Guid Id { get; set; }

    public double Score { get; set; }
}

/// <summary>
/// Building blocks shared by the listing and the store search: how well a
/// word matches a text, and running the generated SQL.
/// </summary>
internal static class SearchSql
{
    /// <summary>More than this many matches are never useful to a person.</summary>
    public const int MaximumCandidates = 400;

    /// <summary>
    /// Adds one parameter per alternative of <paramref name="token"/> and
    /// returns their names.
    /// </summary>
    public static List<string> AddTokenParameters(
        List<NpgsqlParameter> parameters,
        int tokenIndex,
        SearchToken token)
    {
        List<string> names = [];

        for (int a = 0; a < token.Alternatives.Count; a++)
        {
            string name = string.Create(
                CultureInfo.InvariantCulture, $"p{tokenIndex}_{a}");

            parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Text)
            {
                Value = token.Alternatives[a]
            });
            names.Add(name);
        }

        return names;
    }

    /// <summary>
    /// SQL for the best match of any alternative of one word in one text,
    /// multiplied by the weight of that text. Only the word the user typed
    /// (the first alternative) is matched with typo tolerance; synonyms must
    /// be spelled right.
    /// </summary>
    public static string Field(
        string column,
        List<string> names,
        double weight,
        bool typoTolerant = true)
    {
        List<string> levels = [];

        for (int i = 0; i < names.Count; i++)
        {
            levels.Add(Level(
                column, names[i], fuzzy: i == 0 && typoTolerant));
        }

        string best = levels.Count == 1
            ? levels[0]
            : $"GREATEST({string.Join(", ", levels)})";

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{best} * {weight}");
    }

    /// <summary>
    /// A cheap test that is true for <b>every</b> text <see cref="Field"/>
    /// can score above zero, and usually for few others. It can use the
    /// trigram (GIN) index on <paramref name="column"/>, so a search reads
    /// the few listings that may match instead of all of them. It may let
    /// through a text that scores zero (the exact scoring decides), but never
    /// drops one that scores above zero.
    /// </summary>
    /// <remarks>
    /// <paramref name="typoTolerant"/> is <c>false</c> for long texts
    /// (descriptions): "similar to some word somewhere in 400 letters" is
    /// true of far too many listings to be useful, and slow.
    /// Whole word, word start and part of a word are all "contains the word".
    /// The typo-tolerant match needs <c>pg_trgm.word_similarity_threshold</c>
    /// at its lowest used value, see <see cref="FuzzyThreshold"/>.
    /// </remarks>
    public static string Gate(
        string column,
        List<string> names,
        bool typoTolerant = true)
    {
        List<string> parts = [];

        foreach (string name in names)
        {
            parts.Add($"{column} LIKE '%'||@{name}||'%'");
        }

        if (typoTolerant)
        {
            parts.Add(
                $"(length(@{names[0]}) >= 4 AND @{names[0]} <% {column})");
        }

        return "(" + string.Join(" OR ", parts) + ")";
    }

    /// <summary>
    /// The lowest word similarity <see cref="Level"/> accepts (words of 7+
    /// letters). The index operator <c>&lt;%</c> compares against one
    /// setting, so it is set to this for the query and the exact scoring
    /// then applies the stricter limits of shorter words.
    /// </summary>
    public const string FuzzyThreshold = "0.4";

    /// <summary>
    /// Runs a search query. Returns <c>null</c> when the database has not
    /// been upgraded for smart search yet (the caller then falls back to the
    /// plain text search).
    /// </summary>
    public static async Task<IReadOnlyList<T>?> RunAsync<T>(
        RentoXDbContext dbContext,
        string sql,
        List<NpgsqlParameter> parameters,
        CancellationToken cancellationToken)
    {
        // SET LOCAL lives only inside a transaction. Join the caller's one
        // when there is one (the setting then ends with it).
        bool ownTransaction = dbContext.Database.CurrentTransaction is null;

        await using var transaction = ownTransaction
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                $"SET LOCAL pg_trgm.word_similarity_threshold = {FuzzyThreshold}",
                cancellationToken);

            List<T> rows = await dbContext.Database
                .SqlQueryRaw<T>(sql, [.. parameters])
                .ToListAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return rows;
        }
        catch (PostgresException exception)
            when (exception.SqlState ==
                  PostgresErrorCodes.UndefinedFunction)
        {
            // rentox_norm / word_similarity missing: migration not applied.
            return null;
        }
    }

    // How well normalized text X matches normalized word P:
    // 1.0 whole word, 0.85 start of a word, 0.6 inside a word (3+ letters),
    // and, for the typed word of 4+ letters, trigram similarity * 0.7 when it
    // is close enough (stricter for short words).
    private static string Level(string x, string p, bool fuzzy)
    {
        string fuzzyBranch = fuzzy
            ? $" WHEN length(@{p}) >= 4 AND word_similarity(@{p}, {x}) >= (CASE WHEN length(@{p}) <= 4 THEN 0.6 WHEN length(@{p}) <= 6 THEN 0.45 ELSE 0.4 END) THEN word_similarity(@{p}, {x}) * 0.7"
            : string.Empty;

        return $"(CASE WHEN {x} = @{p} OR {x} LIKE @{p}||' %' OR {x} LIKE '% '||@{p} OR {x} LIKE '% '||@{p}||' %' THEN 1.0" +
               $" WHEN {x} LIKE @{p}||'%' OR {x} LIKE '% '||@{p}||'%' THEN 0.85" +
               $" WHEN length(@{p}) >= 3 AND {x} LIKE '%'||@{p}||'%' THEN 0.6" +
               fuzzyBranch +
               " ELSE 0 END)";
    }
}
