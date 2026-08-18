using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentoX.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddListingFieldValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "listing_field_values",
                schema: "listings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryFieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    TextValue = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    NumericValue = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    FlagValue = table.Column<bool>(type: "boolean", nullable: true),
                    CalendarValue = table.Column<DateOnly>(type: "date", nullable: true),
                    CustomValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listing_field_values", x => x.Id);
                    table.ForeignKey(
                        name: "FK_listing_field_values_category_fields_CategoryFieldId",
                        column: x => x.CategoryFieldId,
                        principalSchema: "catalog",
                        principalTable: "category_fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_listing_field_values_listings_ListingId",
                        column: x => x.ListingId,
                        principalSchema: "listings",
                        principalTable: "listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "listing_field_selections",
                schema: "listings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingFieldValueId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryFieldOptionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listing_field_selections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_listing_field_selections_category_field_options_CategoryFie~",
                        column: x => x.CategoryFieldOptionId,
                        principalSchema: "catalog",
                        principalTable: "category_field_options",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_listing_field_selections_listing_field_values_ListingFieldV~",
                        column: x => x.ListingFieldValueId,
                        principalSchema: "listings",
                        principalTable: "listing_field_values",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_listing_field_selections_CategoryFieldOptionId",
                schema: "listings",
                table: "listing_field_selections",
                column: "CategoryFieldOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_listing_field_selections_ListingFieldValueId_CategoryFieldO~",
                schema: "listings",
                table: "listing_field_selections",
                columns: new[] { "ListingFieldValueId", "CategoryFieldOptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_listing_field_values_CategoryFieldId_FlagValue",
                schema: "listings",
                table: "listing_field_values",
                columns: new[] { "CategoryFieldId", "FlagValue" });

            migrationBuilder.CreateIndex(
                name: "IX_listing_field_values_CategoryFieldId_NumericValue",
                schema: "listings",
                table: "listing_field_values",
                columns: new[] { "CategoryFieldId", "NumericValue" });

            migrationBuilder.CreateIndex(
                name: "IX_listing_field_values_ListingId_CategoryFieldId",
                schema: "listings",
                table: "listing_field_values",
                columns: new[] { "ListingId", "CategoryFieldId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "listing_field_selections",
                schema: "listings");

            migrationBuilder.DropTable(
                name: "listing_field_values",
                schema: "listings");
        }
    }
}
