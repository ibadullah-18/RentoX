using System.Globalization;
using System.Text;
using Npgsql;
using NpgsqlTypes;
using RentoX.Application.Listings.Search;
using RentoX.Domain.Listings.Enums;
using RentoX.Infrastructure.Persistence;
using RentoX.Infrastructure.Search;

namespace RentoX.Infrastructure.Listings.Search;

/// <summary>One scored listing (mapped from the search SQL).</summary>
public sealed class ListingSearchScore
{
    public Guid Id { get; set; }

    public double Score { get; set; }
}

/// <summary>
/// Finds and ranks listings for a search query inside PostgreSQL.
/// <para>
/// A listing matches when <b>every</b> word of the query is found in its
/// title, category (and parent categories, in every language), searchable
/// field values or description. Each word is matched as a whole word, a word
/// start, a part of a word, a synonym, or (for words of 4+ letters) with
/// typo tolerance (<c>pg_trgm</c> word similarity). The score adds up the
/// best match per word, weighted by where it was found: title counts most.
/// </para>
/// </summary>
public static class ListingSearchScorer
{
    /// <summary>More than this many matches are never useful to a person.</summary>
    public const int MaximumCandidates = SearchSql.MaximumCandidates;

    private const double TitleWeight = 10;
    private const double CategoryWeight = 6;
    private const double FieldsWeight = 5;
    private const double DescriptionWeight = 2;

    /// <summary>
    /// Returns the matching listing ids with their scores (best first), or
    /// <c>null</c> when the database has not been upgraded for smart search
    /// yet (the caller then falls back to the plain text search).
    /// </summary>
    public static async Task<IReadOnlyList<ListingSearchScore>?> ScoreAsync(
        RentoXDbContext dbContext,
        IReadOnlyList<SearchToken> tokens,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(tokens);

        if (tokens.Count == 0)
        {
            return [];
        }

        (string sql, List<NpgsqlParameter> parameters) = BuildSql(tokens, now);

        return await SearchSql.RunAsync<ListingSearchScore>(
            dbContext, sql, parameters, cancellationToken);
    }

    internal static (string Sql, List<NpgsqlParameter> Parameters) BuildSql(
        IReadOnlyList<SearchToken> tokens,
        DateTimeOffset now)
    {
        List<NpgsqlParameter> parameters =
        [
            new("status", NpgsqlDbType.Integer)
            {
                Value = (int)ListingStatus.Active
            },
            new("now", NpgsqlDbType.TimestampTz)
            {
                Value = now.UtcDateTime
            }
        ];

        StringBuilder scored = new();
        StringBuilder sum = new();
        StringBuilder require = new();

        for (int t = 0; t < tokens.Count; t++)
        {
            List<string> names =
                SearchSql.AddTokenParameters(parameters, t, tokens[t]);

            if (t > 0)
            {
                scored.Append(",\n");
                sum.Append(" + ");
                require.Append(" AND ");
            }

            scored.Append(string.Create(
                CultureInfo.InvariantCulture,
                $"({SearchSql.Field("d.title", names, TitleWeight)} + {SearchSql.Field("d.cat", names, CategoryWeight)} + {SearchSql.Field("d.fields", names, FieldsWeight)} + {SearchSql.Field("d.descr", names, DescriptionWeight)}) AS s{t}"));
            sum.Append(string.Create(CultureInfo.InvariantCulture, $"s{t}"));
            require.Append(string.Create(CultureInfo.InvariantCulture, $"s{t} > 0"));
        }

        string sql = string.Create(
            CultureInfo.InvariantCulture,
            $"""
            WITH RECURSIVE chain AS (
              SELECT c."Id" AS leaf, c."Id" AS node, c."ParentId" AS parent
              FROM catalog.categories c
              UNION ALL
              SELECT chain.leaf, p."Id", p."ParentId"
              FROM chain JOIN catalog.categories p ON p."Id" = chain.parent
            ),
            cat_text AS (
              SELECT chain.leaf AS category_id,
                     string_agg(public.rentox_norm(t."Name"), ' ') AS txt
              FROM chain
              JOIN catalog.category_translations t ON t."CategoryId" = chain.node
              GROUP BY chain.leaf
            ),
            field_pieces AS (
              SELECT fv."ListingId" AS listing_id, fv."TextValue" AS piece
              FROM listings.listing_field_values fv
              JOIN catalog.category_fields cf
                ON cf."Id" = fv."CategoryFieldId" AND cf."IsActive" AND cf."IsSearchable"
              WHERE fv."TextValue" IS NOT NULL
              UNION ALL
              SELECT fv."ListingId", fv."CustomValue"
              FROM listings.listing_field_values fv
              JOIN catalog.category_fields cf
                ON cf."Id" = fv."CategoryFieldId" AND cf."IsActive" AND cf."IsSearchable"
              WHERE fv."CustomValue" IS NOT NULL
              UNION ALL
              SELECT fv."ListingId", o."Value"
              FROM listings.listing_field_values fv
              JOIN catalog.category_fields cf
                ON cf."Id" = fv."CategoryFieldId" AND cf."IsActive" AND cf."IsSearchable"
              JOIN listings.listing_field_selections s ON s."ListingFieldValueId" = fv."Id"
              JOIN catalog.category_field_options o
                ON o."Id" = s."CategoryFieldOptionId" AND o."IsActive"
              UNION ALL
              SELECT fv."ListingId", ot."Label"
              FROM listings.listing_field_values fv
              JOIN catalog.category_fields cf
                ON cf."Id" = fv."CategoryFieldId" AND cf."IsActive" AND cf."IsSearchable"
              JOIN listings.listing_field_selections s ON s."ListingFieldValueId" = fv."Id"
              JOIN catalog.category_field_options o
                ON o."Id" = s."CategoryFieldOptionId" AND o."IsActive"
              JOIN catalog.category_field_option_translations ot
                ON ot."CategoryFieldOptionId" = o."Id"
            ),
            field_text AS (
              SELECT listing_id, string_agg(public.rentox_norm(piece), ' ') AS txt
              FROM field_pieces
              GROUP BY listing_id
            ),
            docs AS (
              SELECT l."Id" AS id,
                     public.rentox_norm(l."Title") AS title,
                     public.rentox_norm(l."Description") AS descr,
                     coalesce(ct.txt, '') AS cat,
                     coalesce(ft.txt, '') AS fields
              FROM listings.listings l
              LEFT JOIN cat_text ct ON ct.category_id = l."CategoryId"
              LEFT JOIN field_text ft ON ft.listing_id = l."Id"
              WHERE l."Status" = @status
                AND l."DeletedAtUtc" IS NULL
                AND l."ExpiresAtUtc" > @now
            ),
            scored AS (
              SELECT d.id,
            {scored}
              FROM docs d
            )
            SELECT id AS "Id", ({sum})::double precision AS "Score"
            FROM scored
            WHERE {require}
            ORDER BY 2 DESC
            LIMIT {MaximumCandidates}
            """);

        return (sql, parameters);
    }
}
