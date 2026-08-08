using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentoX.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicCategoryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "category_fields",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsFilterable = table.Column<bool>(type: "boolean", nullable: false),
                    IsSearchable = table.Column<bool>(type: "boolean", nullable: false),
                    AllowCustomValue = table.Column<bool>(type: "boolean", nullable: false),
                    AppliesToDescendants = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_fields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_category_fields_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "catalog",
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "category_field_options",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryFieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_field_options", x => x.Id);
                    table.ForeignKey(
                        name: "FK_category_field_options_category_fields_CategoryFieldId",
                        column: x => x.CategoryFieldId,
                        principalSchema: "catalog",
                        principalTable: "category_fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "category_field_translations",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryFieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    Language = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_field_translations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_category_field_translations_category_fields_CategoryFieldId",
                        column: x => x.CategoryFieldId,
                        principalSchema: "catalog",
                        principalTable: "category_fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "category_field_option_translations",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryFieldOptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Language = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_field_option_translations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_category_field_option_translations_category_field_options_C~",
                        column: x => x.CategoryFieldOptionId,
                        principalSchema: "catalog",
                        principalTable: "category_field_options",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_category_field_option_translations_CategoryFieldOptionId_La~",
                schema: "catalog",
                table: "category_field_option_translations",
                columns: new[] { "CategoryFieldOptionId", "Language" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_field_options_CategoryFieldId_Value",
                schema: "catalog",
                table: "category_field_options",
                columns: new[] { "CategoryFieldId", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_field_translations_CategoryFieldId_Language",
                schema: "catalog",
                table: "category_field_translations",
                columns: new[] { "CategoryFieldId", "Language" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_fields_CategoryId_Key",
                schema: "catalog",
                table: "category_fields",
                columns: new[] { "CategoryId", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "category_field_option_translations",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "category_field_translations",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "category_field_options",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "category_fields",
                schema: "catalog");
        }
    }
}
