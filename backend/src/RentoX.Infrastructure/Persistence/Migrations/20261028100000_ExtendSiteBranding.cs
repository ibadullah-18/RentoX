using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentoX.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtendSiteBranding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AnnouncementEnabled",
                schema: "content",
                table: "site_branding",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AnnouncementLink",
                schema: "content",
                table: "site_branding",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnnouncementText",
                schema: "content",
                table: "site_branding",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AnnouncementTone",
                schema: "content",
                table: "site_branding",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DefaultTheme",
                schema: "content",
                table: "site_branding",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "content",
                table: "site_branding",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MotionLevel",
                schema: "content",
                table: "site_branding",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ShareImageKey",
                schema: "content",
                table: "site_branding",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnnouncementEnabled",
                schema: "content",
                table: "site_branding");

            migrationBuilder.DropColumn(
                name: "AnnouncementLink",
                schema: "content",
                table: "site_branding");

            migrationBuilder.DropColumn(
                name: "AnnouncementText",
                schema: "content",
                table: "site_branding");

            migrationBuilder.DropColumn(
                name: "AnnouncementTone",
                schema: "content",
                table: "site_branding");

            migrationBuilder.DropColumn(
                name: "DefaultTheme",
                schema: "content",
                table: "site_branding");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "content",
                table: "site_branding");

            migrationBuilder.DropColumn(
                name: "MotionLevel",
                schema: "content",
                table: "site_branding");

            migrationBuilder.DropColumn(
                name: "ShareImageKey",
                schema: "content",
                table: "site_branding");
        }
    }
}
