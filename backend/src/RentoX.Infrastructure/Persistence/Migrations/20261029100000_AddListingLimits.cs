using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentoX.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddListingLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxNewListingsPerDay",
                schema: "admin",
                table: "settings_versions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinListingPhotos",
                schema: "admin",
                table: "settings_versions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxNewListingsPerDay",
                schema: "admin",
                table: "settings_versions");

            migrationBuilder.DropColumn(
                name: "MinListingPhotos",
                schema: "admin",
                table: "settings_versions");
        }
    }
}
