using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentoX.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddListingPromotions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "listing_promotions",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ChargedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    WalletTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PurchasedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listing_promotions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_listing_promotions_listings_ListingId",
                        column: x => x.ListingId,
                        principalSchema: "listings",
                        principalTable: "listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_listing_promotions_wallet_transactions_WalletTransactionId",
                        column: x => x.WalletTransactionId,
                        principalSchema: "payments",
                        principalTable: "wallet_transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_listing_promotions_ListingId_Type_EndsAtUtc",
                schema: "payments",
                table: "listing_promotions",
                columns: new[] { "ListingId", "Type", "EndsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_listing_promotions_ListingId_Type_StartsAtUtc",
                schema: "payments",
                table: "listing_promotions",
                columns: new[] { "ListingId", "Type", "StartsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_listing_promotions_WalletTransactionId",
                schema: "payments",
                table: "listing_promotions",
                column: "WalletTransactionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "listing_promotions",
                schema: "payments");
        }
    }
}
