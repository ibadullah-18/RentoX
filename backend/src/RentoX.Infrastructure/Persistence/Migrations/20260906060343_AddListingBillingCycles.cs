using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentoX.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddListingBillingCycles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "listing_billing_cycles",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    CycleNumber = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    PeriodStartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEndUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ChargedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    WalletTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    RefundWalletTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    RefundedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listing_billing_cycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_listing_billing_cycles_listings_ListingId",
                        column: x => x.ListingId,
                        principalSchema: "listings",
                        principalTable: "listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_listing_billing_cycles_wallet_transactions_RefundWalletTran~",
                        column: x => x.RefundWalletTransactionId,
                        principalSchema: "payments",
                        principalTable: "wallet_transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_listing_billing_cycles_wallet_transactions_WalletTransactio~",
                        column: x => x.WalletTransactionId,
                        principalSchema: "payments",
                        principalTable: "wallet_transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_listing_billing_cycles_ListingId_CycleNumber",
                schema: "payments",
                table: "listing_billing_cycles",
                columns: new[] { "ListingId", "CycleNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_listing_billing_cycles_ListingId_PeriodEndUtc",
                schema: "payments",
                table: "listing_billing_cycles",
                columns: new[] { "ListingId", "PeriodEndUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_listing_billing_cycles_RefundWalletTransactionId",
                schema: "payments",
                table: "listing_billing_cycles",
                column: "RefundWalletTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_listing_billing_cycles_WalletTransactionId",
                schema: "payments",
                table: "listing_billing_cycles",
                column: "WalletTransactionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "listing_billing_cycles",
                schema: "payments");
        }
    }
}
