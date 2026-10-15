using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentoX.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// The site's adjustable settings (for now: how long a listing stays on show) as
    /// versions, so an admin can change them without a new release.
    /// </summary>
    [DbContext(typeof(RentoXDbContext))]
    [Migration("20261015120000_AddSettingsVersions")]
    public partial class AddSettingsVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "admin");

            migrationBuilder.CreateTable(
                name: "settings_versions",
                schema: "admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingLifetimeDays = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_settings_versions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_settings_versions_CreatedAtUtc",
                schema: "admin",
                table: "settings_versions",
                column: "CreatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "settings_versions",
                schema: "admin");
        }
    }
}
