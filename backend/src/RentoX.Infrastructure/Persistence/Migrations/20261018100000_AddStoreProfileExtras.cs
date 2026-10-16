using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentoX.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreProfileExtras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WhatsAppMessage",
                schema: "stores",
                table: "store_profiles",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppNumber",
                schema: "stores",
                table: "store_profiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkingHours",
                schema: "stores",
                table: "store_profiles",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YoutubeUrl",
                schema: "stores",
                table: "store_profiles",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "store_day_stats",
                schema: "stores",
                columns: table => new
                {
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    Views = table.Column<int>(type: "integer", nullable: false),
                    Calls = table.Column<int>(type: "integer", nullable: false),
                    WhatsApp = table.Column<int>(type: "integer", nullable: false),
                    Messages = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_store_day_stats", x => new { x.StoreId, x.Day });
                    table.ForeignKey(
                        name: "FK_store_day_stats_store_profiles_StoreId",
                        column: x => x.StoreId,
                        principalSchema: "stores",
                        principalTable: "store_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "store_day_stats",
                schema: "stores");

            migrationBuilder.DropColumn(
                name: "WhatsAppMessage",
                schema: "stores",
                table: "store_profiles");

            migrationBuilder.DropColumn(
                name: "WhatsAppNumber",
                schema: "stores",
                table: "store_profiles");

            migrationBuilder.DropColumn(
                name: "WorkingHours",
                schema: "stores",
                table: "store_profiles");

            migrationBuilder.DropColumn(
                name: "YoutubeUrl",
                schema: "stores",
                table: "store_profiles");
        }
    }
}
