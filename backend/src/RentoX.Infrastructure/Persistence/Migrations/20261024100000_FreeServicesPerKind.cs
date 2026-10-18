using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentoX.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FreeServicesPerKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Free services are now chosen one by one (bits: 1 listing fee, 2 VIP, 4 bump, 8 VIP store).
            // Whoever had "everything free" keeps all four.
            migrationBuilder.Sql("ALTER TABLE users.user_profiles ALTER COLUMN \"FreeServices\" DROP DEFAULT;");
            migrationBuilder.Sql(
                "ALTER TABLE users.user_profiles ALTER COLUMN \"FreeServices\" TYPE integer " +
                "USING CASE WHEN \"FreeServices\" THEN 15 ELSE 0 END;");
            migrationBuilder.Sql("ALTER TABLE users.user_profiles ALTER COLUMN \"FreeServices\" SET DEFAULT 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE users.user_profiles ALTER COLUMN \"FreeServices\" DROP DEFAULT;");
            migrationBuilder.Sql(
                "ALTER TABLE users.user_profiles ALTER COLUMN \"FreeServices\" TYPE boolean " +
                "USING (\"FreeServices\" <> 0);");
            migrationBuilder.Sql("ALTER TABLE users.user_profiles ALTER COLUMN \"FreeServices\" SET DEFAULT false;");
        }
    }
}
