using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentoX.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "support_ticket_images",
                schema: "support",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_support_ticket_images", x => x.Id);
                    table.CheckConstraint("CK_support_ticket_images_DisplayOrder", "\"DisplayOrder\" >= 0 AND \"DisplayOrder\" < 5");
                    table.CheckConstraint("CK_support_ticket_images_SizeBytes", "\"SizeBytes\" > 0 AND \"SizeBytes\" <= 10485760");
                    table.ForeignKey(
                        name: "FK_support_ticket_images_support_ticket_messages_MessageId",
                        column: x => x.MessageId,
                        principalSchema: "support",
                        principalTable: "support_ticket_messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_support_ticket_images_MessageId_DisplayOrder",
                schema: "support",
                table: "support_ticket_images",
                columns: new[] { "MessageId", "DisplayOrder" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "support_ticket_images",
                schema: "support");
        }
    }
}
