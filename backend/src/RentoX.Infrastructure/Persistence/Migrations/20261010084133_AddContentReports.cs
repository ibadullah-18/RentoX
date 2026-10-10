using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentoX.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContentReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "moderation");

            migrationBuilder.CreateTable(
                name: "content_reports",
                schema: "moderation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<int>(type: "integer", nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<int>(type: "integer", nullable: false),
                    Details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolutionNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_content_reports_users_ReporterId",
                        column: x => x.ReporterId,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_content_reports_users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_content_reports_ReporterId",
                schema: "moderation",
                table: "content_reports",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_content_reports_ReviewedByUserId",
                schema: "moderation",
                table: "content_reports",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_content_reports_Status_CreatedAtUtc",
                schema: "moderation",
                table: "content_reports",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_content_reports_TargetType_TargetId",
                schema: "moderation",
                table: "content_reports",
                columns: new[] { "TargetType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_content_reports_TargetType_TargetId_ReporterId",
                schema: "moderation",
                table: "content_reports",
                columns: new[] { "TargetType", "TargetId", "ReporterId" },
                unique: true,
                filter: "\"Status\" IN (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content_reports",
                schema: "moderation");
        }
    }
}
