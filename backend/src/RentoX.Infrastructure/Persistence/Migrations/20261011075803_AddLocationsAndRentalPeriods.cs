using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentoX.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationsAndRentalPeriods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "locations");

            migrationBuilder.AddColumn<Guid>(
                name: "CityId",
                schema: "listings",
                table: "listings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DistrictId",
                schema: "listings",
                table: "listings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RentalPeriodMask",
                schema: "catalog",
                table: "categories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "cities",
                schema: "locations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NameAz = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NameRu = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "districts",
                schema: "locations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    NameAz = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NameRu = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_districts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_districts_cities_CityId",
                        column: x => x.CityId,
                        principalSchema: "locations",
                        principalTable: "cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_listings_CityId_DistrictId",
                schema: "listings",
                table: "listings",
                columns: new[] { "CityId", "DistrictId" });

            migrationBuilder.CreateIndex(
                name: "IX_listings_DistrictId",
                schema: "listings",
                table: "listings",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_cities_NameAz",
                schema: "locations",
                table: "cities",
                column: "NameAz",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_districts_CityId_NameAz",
                schema: "locations",
                table: "districts",
                columns: new[] { "CityId", "NameAz" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_listings_cities_CityId",
                schema: "listings",
                table: "listings",
                column: "CityId",
                principalSchema: "locations",
                principalTable: "cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_listings_districts_DistrictId",
                schema: "listings",
                table: "listings",
                column: "DistrictId",
                principalSchema: "locations",
                principalTable: "districts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Reference data: cities and districts (Baku and Ganja have
            // districts). Fixed ids, so it can be run safely again.
            migrationBuilder.Sql(
                """
                INSERT INTO locations.cities ("Id","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('49aca551-f2a8-5cd7-b3a6-57f74c5b9519','Bakı','Баку','Baku',1,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.districts ("Id","CityId","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('619d498b-bec7-5da1-a63c-10ced0c69925','49aca551-f2a8-5cd7-b3a6-57f74c5b9519','Binəqədi','Бинагадинский','Binagadi',1,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.districts ("Id","CityId","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('e3da1d55-618a-5a42-8560-72987007f9d2','49aca551-f2a8-5cd7-b3a6-57f74c5b9519','Xətai','Хатаи','Khatai',2,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.districts ("Id","CityId","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('fd137251-4c7a-59dd-b61e-bdda952936d8','49aca551-f2a8-5cd7-b3a6-57f74c5b9519','Xəzər','Хазар','Khazar',3,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.districts ("Id","CityId","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('c3ee4bd3-700e-5454-810a-55677718190f','49aca551-f2a8-5cd7-b3a6-57f74c5b9519','Nərimanov','Нариманов','Narimanov',4,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.districts ("Id","CityId","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('27235e73-763e-5055-910e-31f151d103e0','49aca551-f2a8-5cd7-b3a6-57f74c5b9519','Nəsimi','Насими','Nasimi',5,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.districts ("Id","CityId","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('9a1863d4-9736-5f85-a9d9-33f7167aeba9','49aca551-f2a8-5cd7-b3a6-57f74c5b9519','Nizami','Низами','Nizami',6,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.districts ("Id","CityId","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('37103cd9-eed0-5f65-8389-0a8a20fb0139','49aca551-f2a8-5cd7-b3a6-57f74c5b9519','Pirallahı','Пираллахи','Pirallahi',7,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.districts ("Id","CityId","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('4e2f4f6a-0e78-5263-b911-d4a8ffcc06e5','49aca551-f2a8-5cd7-b3a6-57f74c5b9519','Qaradağ','Гарадаг','Garadagh',8,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.districts ("Id","CityId","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('896ccf56-39de-5732-847b-e9c9f7b69136','49aca551-f2a8-5cd7-b3a6-57f74c5b9519','Sabunçu','Сабунчу','Sabunchu',9,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.districts ("Id","CityId","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('a4dd90e7-be3d-5138-b278-c8692a46a0ac','49aca551-f2a8-5cd7-b3a6-57f74c5b9519','Səbail','Сабаиль','Sabail',10,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.districts ("Id","CityId","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('0abea293-ce7a-5b2a-8fb4-93772721a1f3','49aca551-f2a8-5cd7-b3a6-57f74c5b9519','Suraxanı','Сураханы','Surakhani',11,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.districts ("Id","CityId","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('565c6802-abcb-56a7-9337-7354aa800f15','49aca551-f2a8-5cd7-b3a6-57f74c5b9519','Yasamal','Ясамал','Yasamal',12,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.cities ("Id","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('5295a35b-8a02-5a4b-942c-a4dcc9d5d891','Gəncə','Гянджа','Ganja',2,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.districts ("Id","CityId","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('d527553d-cec1-5ecf-82e7-3bb33e777651','5295a35b-8a02-5a4b-942c-a4dcc9d5d891','Kəpəz','Кяпаз','Kapaz',1,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.districts ("Id","CityId","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('c5e23051-2f66-58f0-914a-aec0a1106dc7','5295a35b-8a02-5a4b-942c-a4dcc9d5d891','Nizami','Низами','Nizami',2,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.cities ("Id","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('e8ebb399-de9d-5acf-bb26-10cd4a985331','Sumqayıt','Сумгаит','Sumgait',3,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.cities ("Id","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('46d30b76-2db6-5f67-bfc3-b9cbccc074d6','Abşeron','Апшерон','Absheron',4,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.cities ("Id","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('efc62eb4-a0e7-5fc4-a730-9f11fc9557a7','Lənkəran','Ленкорань','Lankaran',5,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.cities ("Id","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('5cde6944-9751-5bc3-abc2-63fbc1d5325b','Mingəçevir','Мингечевир','Mingachevir',6,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.cities ("Id","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('28d63155-3f82-5b5b-91d5-c3eb6f9abf8a','Şirvan','Ширван','Shirvan',7,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.cities ("Id","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('a98cff56-9718-5757-aafe-1dd115836d1a','Naxçıvan','Нахчыван','Nakhchivan',8,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.cities ("Id","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('8a892178-49e6-5c04-b37b-c2db28be497a','Şəki','Шеки','Sheki',9,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.cities ("Id","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('6c40a479-4a08-5c03-8255-c94c1fbc38fc','Quba','Куба','Quba',10,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.cities ("Id","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('c57b5532-6986-5375-8efe-1cff72571334','Qusar','Гусар','Gusar',11,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.cities ("Id","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('1b9fb305-ce76-5430-b039-501a2fc6c6ca','Xaçmaz','Хачмаз','Khachmaz',12,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.cities ("Id","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('34b1e14e-3a86-5680-989d-a4797fb59a80','Qəbələ','Габала','Gabala',13,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.cities ("Id","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('f1261fc2-8983-58f6-9921-507a5f9ba0bb','İsmayıllı','Исмаиллы','Ismayilli',14,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.cities ("Id","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('61af4830-1ddf-5225-bd3b-25db6795ab90','Şamaxı','Шамахы','Shamakhi',15,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.cities ("Id","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('08ad6986-7164-51cf-a462-df77a23be6c8','Yevlax','Евлах','Yevlakh',16,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.cities ("Id","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('f814d54c-92a4-5850-8714-09bf64b45f70','Zaqatala','Закатала','Zagatala',17,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.cities ("Id","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('e185b969-260e-5544-b50f-2d4e371fffb8','Bərdə','Барда','Barda',18,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.cities ("Id","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('c84a4a49-a1fc-5258-ab91-c63ff678a3be','Masallı','Масаллы','Masalli',19,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.cities ("Id","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('b9ee9238-becc-56cb-ac36-268fdd7e55dc','Astara','Астара','Astara',20,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.cities ("Id","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('c34344e5-526e-5c7a-88a0-746bffbdbff2','Göyçay','Гёйчай','Goychay',21,true) ON CONFLICT DO NOTHING;
                INSERT INTO locations.cities ("Id","NameAz","NameRu","NameEn","DisplayOrder","IsActive") VALUES ('fb03332e-c1e5-5dbb-aa0a-db8c93565ce0','Salyan','Сальян','Salyan',22,true) ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_listings_cities_CityId",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropForeignKey(
                name: "FK_listings_districts_DistrictId",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropTable(
                name: "districts",
                schema: "locations");

            migrationBuilder.DropTable(
                name: "cities",
                schema: "locations");

            migrationBuilder.DropIndex(
                name: "IX_listings_CityId_DistrictId",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropIndex(
                name: "IX_listings_DistrictId",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "CityId",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "DistrictId",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "RentalPeriodMask",
                schema: "catalog",
                table: "categories");
        }
    }
}
