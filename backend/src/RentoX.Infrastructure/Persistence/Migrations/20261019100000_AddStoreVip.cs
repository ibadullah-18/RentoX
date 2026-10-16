using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentoX.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreVip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "store_vip_plans",
                schema: "stores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DurationDays = table.Column<int>(type: "integer", nullable: false),
                    OfferMessage = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    OfferIntervalHours = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_store_vip_plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "store_vip_purchases",
                schema: "stores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanName = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    ChargedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    WalletTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_store_vip_purchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_store_vip_purchases_store_profiles_StoreId",
                        column: x => x.StoreId,
                        principalSchema: "stores",
                        principalTable: "store_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_store_vip_purchases_wallet_transactions_WalletTransactionId",
                        column: x => x.WalletTransactionId,
                        principalSchema: "payments",
                        principalTable: "wallet_transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_store_vip_plans_CreatedAtUtc",
                schema: "stores",
                table: "store_vip_plans",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_store_vip_purchases_StoreId_EndsAtUtc",
                schema: "stores",
                table: "store_vip_purchases",
                columns: new[] { "StoreId", "EndsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_store_vip_purchases_WalletTransactionId",
                schema: "stores",
                table: "store_vip_purchases",
                column: "WalletTransactionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "store_vip_plans",
                schema: "stores");

            migrationBuilder.DropTable(
                name: "store_vip_purchases",
                schema: "stores");
        }
    }
}

