using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentoX.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Trigram (GIN) indexes on the normalized listing title and description,
    /// so searching does not have to read every listing. Changes no tables,
    /// so the model snapshot is unchanged.
    /// </summary>
    [DbContext(typeof(RentoXDbContext))]
    [Migration("20261012090000_AddSearchIndexes")]
    public partial class AddSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS ix_listings_title_trgm
                ON listings.listings
                USING gin (public.rentox_norm("Title") gin_trgm_ops);
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS ix_listings_description_trgm
                ON listings.listings
                USING gin (public.rentox_norm("Description") gin_trgm_ops);
                """);

            // Field values (free text and custom entries) are searched too.
            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS ix_listing_field_values_text_trgm
                ON listings.listing_field_values
                USING gin (public.rentox_norm("TextValue") gin_trgm_ops);
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS ix_listing_field_values_custom_trgm
                ON listings.listing_field_values
                USING gin (public.rentox_norm("CustomValue") gin_trgm_ops);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS listings.ix_listing_field_values_custom_trgm;");
            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS listings.ix_listing_field_values_text_trgm;");
            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS listings.ix_listings_description_trgm;");
            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS listings.ix_listings_title_trgm;");
        }
    }
}
