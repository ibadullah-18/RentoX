using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentoX.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteBranding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "site_branding",
                schema: "content",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FooterText = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UseTextLogo = table.Column<bool>(type: "boolean", nullable: false),
                    HeaderLogoHeight = table.Column<int>(type: "integer", nullable: false, defaultValue: 40),
                    FooterLogoHeight = table.Column<int>(type: "integer", nullable: false, defaultValue: 40),
                    HeaderLogoKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    HeaderLogoDarkKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    FooterLogoKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    FooterLogoDarkKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    IconKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_site_branding", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "site_branding",
                schema: "content");
        }
    }
}
