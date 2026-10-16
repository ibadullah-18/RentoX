using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentoX.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffSalaryHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "staff_salary_changes",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    AmountInQepik = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_salary_changes", x => x.Id);
                    table.CheckConstraint("CK_staff_salary_changes_Amount", "\"AmountInQepik\" BETWEEN 0 AND 10000000");
                    table.ForeignKey(
                        name: "FK_staff_salary_changes_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_staff_salary_changes_UserId_EffectiveFrom",
                schema: "hr",
                table: "staff_salary_changes",
                columns: new[] { "UserId", "EffectiveFrom" });

            // Today's salary of every person already on the list starts from the month their profile was made.
            migrationBuilder.Sql(
                @"INSERT INTO hr.staff_salary_changes (""Id"", ""UserId"", ""EffectiveFrom"", ""AmountInQepik"", ""CreatedByUserId"", ""CreatedAtUtc"")
                  SELECT gen_random_uuid(), ""UserId"",
                         date_trunc('month', ""CreatedAtUtc"" AT TIME ZONE 'Asia/Baku')::date,
                         ""MonthlySalaryInQepik"", ""UserId"", now()
                  FROM hr.staff_profiles
                  WHERE ""MonthlySalaryInQepik"" > 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "staff_salary_changes",
                schema: "hr");
        }
    }
}
