using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentoX.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Banners for the top of the home page, managed by the staff: a picture, a text in three
    /// languages, an optional link, an order and optional dates.
    /// </summary>
    [DbContext(typeof(RentoXDbContext))]
    [Migration("20261015160000_AddHomeBanners")]
    public partial class AddHomeBanners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "content");

            migrationBuilder.CreateTable(
                name: "home_banners",
                schema: "content",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TitleAz = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TitleRu = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TitleEn = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SubtitleAz = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SubtitleRu = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SubtitleEn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LinkUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ImageKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_home_banners", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_home_banners_IsActive_DisplayOrder",
                schema: "content",
                table: "home_banners",
                columns: new[] { "IsActive", "DisplayOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "home_banners",
                schema: "content");
        }
    }
}
