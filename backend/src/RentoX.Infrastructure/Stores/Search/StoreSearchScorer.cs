using System.Globalization;
using System.Text;
using Npgsql;
using NpgsqlTypes;
using RentoX.Application.Listings.Search;
using RentoX.Domain.Stores.Enums;
using RentoX.Infrastructure.Persistence;
using RentoX.Infrastructure.Search;

namespace RentoX.Infrastructure.Stores.Search;

/// <summary>One scored store (mapped from the search SQL).</summary>
public sealed class StoreSearchScore
{
    public Guid Id { get; set; }

    public double Score { get; set; }
}

/// <summary>
/// Finds and ranks active stores by name, description and address with the
/// same word matching as the listing search (typos, synonyms, plurals).
/// Every word of the query has to be found.
/// </summary>
public static class StoreSearchScorer
{
    private const double NameWeight = 10;
    private const double DescriptionWeight = 3;
    private const double AddressWeight = 2;

    /// <summary>
    /// Returns the matching store ids with scores (best first), or
    /// <c>null</c> when the database has not been upgraded for smart search.
    /// </summary>
    public static async Task<IReadOnlyList<StoreSearchScore>?> ScoreAsync(
        RentoXDbContext dbContext,
        IReadOnlyList<SearchToken> tokens,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(tokens);

        if (tokens.Count == 0)
        {
            return [];
        }

        (string sql, List<NpgsqlParameter> parameters) = BuildSql(tokens);

        return await SearchSql.RunAsync<StoreSearchScore>(
            dbContext, sql, parameters, cancellationToken);
    }

    internal static (string Sql, List<NpgsqlParameter> Parameters) BuildSql(
        IReadOnlyList<SearchToken> tokens)
    {
        List<NpgsqlParameter> parameters =
        [
            new("status", NpgsqlDbType.Integer)
            {
                Value = (int)StoreStatus.Active
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
                $"({SearchSql.Field("d.name", names, NameWeight)} + {SearchSql.Field("d.descr", names, DescriptionWeight)} + {SearchSql.Field("d.addr", names, AddressWeight)}) AS s{t}"));
            sum.Append(string.Create(CultureInfo.InvariantCulture, $"s{t}"));
            require.Append(string.Create(CultureInfo.InvariantCulture, $"s{t} > 0"));
        }

        string sql = string.Create(
            CultureInfo.InvariantCulture,
            $"""
            WITH docs AS (
              SELECT s."Id" AS id,
                     public.rentox_norm(s."Name") AS name,
                     public.rentox_norm(s."Description") AS descr,
                     public.rentox_norm(s."Address") AS addr
              FROM stores.store_profiles s
              WHERE s."Status" = @status
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
            LIMIT {SearchSql.MaximumCandidates}
            """);

        return (sql, parameters);
    }
}
