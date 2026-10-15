using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentoX.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// The pages of the site and app that the staff edit (terms, privacy, about, cooperation, rules, FAQ)
    /// and the operator's details with the contact channels shown in the footer.
    /// </summary>
    [DbContext(typeof(RentoXDbContext))]
    [Migration("20261016100000_AddContentPages")]
    public partial class AddContentPages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "content");

            migrationBuilder.CreateTable(
                name: "pages",
                schema: "content",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TitleAz = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TitleRu = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TitleEn = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    BodyAz = table.Column<string>(type: "text", nullable: false),
                    BodyRu = table.Column<string>(type: "text", nullable: false),
                    BodyEn = table.Column<string>(type: "text", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pages_Slug",
                schema: "content",
                table: "pages",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateTable(
                name: "site_info",
                schema: "content",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatorName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Voen = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Phone = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    Hotline = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    WhatsApp = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Instagram = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    TikTok = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Facebook = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_site_info", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "site_info",
                schema: "content");

            migrationBuilder.DropTable(
                name: "pages",
                schema: "content");
        }
    }
}
