using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentoX.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignPages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DarkOverlay",
                schema: "content",
                table: "home_banners",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "OpenInNewTab",
                schema: "content",
                table: "home_banners",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PageBodyAz",
                schema: "content",
                table: "home_banners",
                type: "character varying(6000)",
                maxLength: 6000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PageBodyEn",
                schema: "content",
                table: "home_banners",
                type: "character varying(6000)",
                maxLength: 6000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PageBodyRu",
                schema: "content",
                table: "home_banners",
                type: "character varying(6000)",
                maxLength: 6000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PageButtonLabelAz",
                schema: "content",
                table: "home_banners",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PageButtonLabelEn",
                schema: "content",
                table: "home_banners",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PageButtonLabelRu",
                schema: "content",
                table: "home_banners",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PageButtonUrl",
                schema: "content",
                table: "home_banners",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PageSlug",
                schema: "content",
                table: "home_banners",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Platform",
                schema: "content",
                table: "home_banners",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<bool>(
                name: "ShowText",
                schema: "content",
                table: "home_banners",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "TextAlign",
                schema: "content",
                table: "home_banners",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TextTone",
                schema: "content",
                table: "home_banners",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_home_banners_PageSlug",
                schema: "content",
                table: "home_banners",
                column: "PageSlug",
                unique: true,
                filter: "\"PageSlug\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_home_banners_PageSlug",
                schema: "content",
                table: "home_banners");

            migrationBuilder.DropColumn(
                name: "DarkOverlay",
                schema: "content",
                table: "home_banners");

            migrationBuilder.DropColumn(
                name: "OpenInNewTab",
                schema: "content",
                table: "home_banners");

            migrationBuilder.DropColumn(
                name: "PageBodyAz",
                schema: "content",
                table: "home_banners");

            migrationBuilder.DropColumn(
                name: "PageBodyEn",
                schema: "content",
                table: "home_banners");

            migrationBuilder.DropColumn(
                name: "PageBodyRu",
                schema: "content",
                table: "home_banners");

            migrationBuilder.DropColumn(
                name: "PageButtonLabelAz",
                schema: "content",
                table: "home_banners");

            migrationBuilder.DropColumn(
                name: "PageButtonLabelEn",
                schema: "content",
                table: "home_banners");

            migrationBuilder.DropColumn(
                name: "PageButtonLabelRu",
                schema: "content",
                table: "home_banners");

            migrationBuilder.DropColumn(
                name: "PageButtonUrl",
                schema: "content",
                table: "home_banners");

            migrationBuilder.DropColumn(
                name: "PageSlug",
                schema: "content",
                table: "home_banners");

            migrationBuilder.DropColumn(
                name: "Platform",
                schema: "content",
                table: "home_banners");

            migrationBuilder.DropColumn(
                name: "ShowText",
                schema: "content",
                table: "home_banners");

            migrationBuilder.DropColumn(
                name: "TextAlign",
                schema: "content",
                table: "home_banners");

            migrationBuilder.DropColumn(
                name: "TextTone",
                schema: "content",
                table: "home_banners");
        }
    }
}
