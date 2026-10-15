using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentoX.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// A person can stop the group messages the staff send (everyone, store
    /// owners, sellers). Everybody starts with them on.
    /// </summary>
    [DbContext(typeof(RentoXDbContext))]
    [Migration("20261015110000_AddAnnouncementsOptOut")]
    public partial class AddAnnouncementsOptOut : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AnnouncementsEnabled",
                schema: "users",
                table: "user_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnnouncementsEnabled",
                schema: "users",
                table: "user_profiles");
        }
    }
}
