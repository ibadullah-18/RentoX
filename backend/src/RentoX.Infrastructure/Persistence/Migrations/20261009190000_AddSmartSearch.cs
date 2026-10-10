using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentoX.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Smart listing search: typo tolerance (pg_trgm) and one normalization
    /// function used for both stored text and the search words.
    /// Changes no tables, so the model snapshot is unchanged.
    /// </summary>
    [DbContext(typeof(RentoXDbContext))]
    [Migration("20261009190000_AddSmartSearch")]
    public partial class AddSmartSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            // Must stay identical to ListingSearchTerms.Normalize in the
            // Application project: lower case, Azerbaijani letters folded to
            // Latin (ə→e, ı→i, ö→o, ü→u, ğ→g, ç→c, ş→s), "ё"→"е", and every
            // run of non letters/digits becomes one space.
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION public.rentox_norm(value text)
                RETURNS text
                LANGUAGE sql
                IMMUTABLE
                PARALLEL SAFE
                AS $$
                  SELECT btrim(regexp_replace(
                    lower(translate(coalesce(value, ''),
                      'ƏəıİÖöÜüĞğÇçŞşЁё',
                      'eeiioouuggccssее')),
                    '[^[:alnum:]]+', ' ', 'g'))
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP FUNCTION IF EXISTS public.rentox_norm(text);");

            // pg_trgm is left installed: other features may rely on it.
        }
    }
}
