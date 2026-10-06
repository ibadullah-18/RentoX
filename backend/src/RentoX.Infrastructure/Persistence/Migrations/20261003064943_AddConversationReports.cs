using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentoX.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "conversation_reports",
                schema: "messaging",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<int>(type: "integer", nullable: false),
                    Details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EvidenceMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolutionNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversation_reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_conversation_reports_conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalSchema: "messaging",
                        principalTable: "conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_conversation_reports_messages_EvidenceMessageId",
                        column: x => x.EvidenceMessageId,
                        principalSchema: "messaging",
                        principalTable: "messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_conversation_reports_users_ReporterId",
                        column: x => x.ReporterId,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_conversation_reports_users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_conversation_reports_ConversationId_ReporterId",
                schema: "messaging",
                table: "conversation_reports",
                columns: new[] { "ConversationId", "ReporterId" },
                unique: true,
                filter: "\"Status\" IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_conversation_reports_EvidenceMessageId",
                schema: "messaging",
                table: "conversation_reports",
                column: "EvidenceMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_conversation_reports_ReporterId",
                schema: "messaging",
                table: "conversation_reports",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_conversation_reports_ReviewedByUserId",
                schema: "messaging",
                table: "conversation_reports",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_conversation_reports_Status_CreatedAtUtc",
                schema: "messaging",
                table: "conversation_reports",
                columns: new[] { "Status", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "conversation_reports",
                schema: "messaging");
        }
    }
}
