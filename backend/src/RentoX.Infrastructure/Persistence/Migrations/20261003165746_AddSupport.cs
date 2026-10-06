using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentoX.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "support");

            migrationBuilder.CreateTable(
                name: "support_tickets",
                schema: "support",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_support_tickets", x => x.Id);
                    table.CheckConstraint("CK_support_tickets_Category", "\"Category\" >= 1 AND \"Category\" <= 7");
                    table.CheckConstraint("CK_support_tickets_Priority", "\"Priority\" >= 1 AND \"Priority\" <= 4");
                    table.CheckConstraint("CK_support_tickets_Status", "\"Status\" >= 1 AND \"Status\" <= 4");
                    table.ForeignKey(
                        name: "FK_support_tickets_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "support_ticket_messages",
                schema: "support",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupportTicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    SentAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_support_ticket_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_support_ticket_messages_support_tickets_SupportTicketId",
                        column: x => x.SupportTicketId,
                        principalSchema: "support",
                        principalTable: "support_tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_support_ticket_messages_users_SenderId",
                        column: x => x.SenderId,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_support_ticket_messages_SenderId",
                schema: "support",
                table: "support_ticket_messages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_support_ticket_messages_SupportTicketId_SentAtUtc_Id",
                schema: "support",
                table: "support_ticket_messages",
                columns: new[] { "SupportTicketId", "SentAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_support_tickets_Status_Priority_UpdatedAtUtc",
                schema: "support",
                table: "support_tickets",
                columns: new[] { "Status", "Priority", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_support_tickets_UserId_UpdatedAtUtc",
                schema: "support",
                table: "support_tickets",
                columns: new[] { "UserId", "UpdatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "support_ticket_messages",
                schema: "support");

            migrationBuilder.DropTable(
                name: "support_tickets",
                schema: "support");
        }
    }
}
