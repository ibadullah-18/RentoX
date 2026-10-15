using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentoX.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// How many photos one listing may have is an adjustable setting too (it was fixed at 15).
    /// The versions already saved keep 15.
    /// </summary>
    [DbContext(typeof(RentoXDbContext))]
    [Migration("20261017100000_AddMaxListingPhotosSetting")]
    public partial class AddMaxListingPhotosSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxListingPhotos",
                schema: "admin",
                table: "settings_versions",
                type: "integer",
                nullable: false,
                defaultValue: 15);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxListingPhotos",
                schema: "admin",
                table: "settings_versions");
        }
    }
}
