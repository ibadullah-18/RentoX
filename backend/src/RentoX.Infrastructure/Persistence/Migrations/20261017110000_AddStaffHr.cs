using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentoX.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffHr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "hr");

            migrationBuilder.CreateTable(
                name: "attendance_alerts",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_alerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_attendance_alerts_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "attendance_records",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    CheckInAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CheckOutAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_attendance_records_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "leave_requests",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromDay = table.Column<DateOnly>(type: "date", nullable: false),
                    ToDay = table.Column<DateOnly>(type: "date", nullable: false),
                    WorkingDays = table.Column<int>(type: "integer", nullable: false),
                    OverLimit = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DecisionNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DecidedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecidedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_requests", x => x.Id);
                    table.CheckConstraint("CK_leave_requests_Status", "\"Status\" BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_leave_requests_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payroll_entries",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Month = table.Column<DateOnly>(type: "date", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    AmountInQepik = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    VoidedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    VoidedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_entries", x => x.Id);
                    table.CheckConstraint("CK_payroll_entries_Amount", "\"AmountInQepik\" > 0");
                    table.CheckConstraint("CK_payroll_entries_Kind", "\"Kind\" BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_payroll_entries_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "staff_profiles",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MonthlySalaryInQepik = table.Column<int>(type: "integer", nullable: false),
                    LoginAllowed = table.Column<bool>(type: "boolean", nullable: false),
                    WorkStartMinute = table.Column<int>(type: "integer", nullable: false),
                    WorkEndMinute = table.Column<int>(type: "integer", nullable: false),
                    WorkDaysMask = table.Column<int>(type: "integer", nullable: false),
                    MonthlyDayOffLimit = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_profiles", x => x.Id);
                    table.CheckConstraint("CK_staff_profiles_Days", "\"WorkDaysMask\" BETWEEN 0 AND 127 AND \"MonthlyDayOffLimit\" BETWEEN 0 AND 31");
                    table.CheckConstraint("CK_staff_profiles_Hours", "\"WorkStartMinute\" BETWEEN 0 AND 1439 AND \"WorkEndMinute\" BETWEEN 1 AND 1440 AND \"WorkEndMinute\" > \"WorkStartMinute\"");
                    table.CheckConstraint("CK_staff_profiles_Salary", "\"MonthlySalaryInQepik\" BETWEEN 0 AND 10000000");
                    table.ForeignKey(
                        name: "FK_staff_profiles_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "staff_requests",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ToOwner = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Answer = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    HandledByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    HandledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_requests", x => x.Id);
                    table.CheckConstraint("CK_staff_requests_Status", "\"Status\" BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_staff_requests_users_FromUserId",
                        column: x => x.FromUserId,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_alerts_UserId_Day",
                schema: "hr",
                table: "attendance_alerts",
                columns: new[] { "UserId", "Day" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_Day",
                schema: "hr",
                table: "attendance_records",
                column: "Day");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_UserId_Day",
                schema: "hr",
                table: "attendance_records",
                columns: new[] { "UserId", "Day" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_leave_requests_Status",
                schema: "hr",
                table: "leave_requests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_leave_requests_UserId_FromDay",
                schema: "hr",
                table: "leave_requests",
                columns: new[] { "UserId", "FromDay" });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_entries_UserId_Month",
                schema: "hr",
                table: "payroll_entries",
                columns: new[] { "UserId", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_staff_profiles_UserId",
                schema: "hr",
                table: "staff_profiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_staff_requests_FromUserId",
                schema: "hr",
                table: "staff_requests",
                column: "FromUserId");

            migrationBuilder.CreateIndex(
                name: "IX_staff_requests_Status_CreatedAtUtc",
                schema: "hr",
                table: "staff_requests",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.Sql("ALTER TABLE audit.entries DROP CONSTRAINT \"CK_audit_entries_Action\";");
            migrationBuilder.Sql("ALTER TABLE audit.entries DROP CONSTRAINT \"CK_audit_entries_Payload\";");

            migrationBuilder.Sql("ALTER TABLE audit.entries ADD CONSTRAINT \"CK_audit_entries_Action\" CHECK (\"Action\" BETWEEN 1 AND 12);");
            migrationBuilder.Sql(
                "ALTER TABLE audit.entries ADD CONSTRAINT \"CK_audit_entries_Payload\" CHECK (" + """
(
    "Action" BETWEEN 1 AND 4
    AND "PreviousValue" IS NOT NULL
    AND "CurrentValue" IS NOT NULL
    AND "PreviousValue" <> "CurrentValue"
    AND "RelatedEntityId" IS NULL
    AND (
        ("Action" = 1
            AND "PreviousValue" BETWEEN 1 AND 8
            AND "CurrentValue" BETWEEN 1 AND 8)
        OR ("Action" = 2
            AND "PreviousValue" BETWEEN 1 AND 6
            AND "CurrentValue" BETWEEN 1 AND 6)
        OR ("Action" IN (3, 4)
            AND "PreviousValue" BETWEEN 1 AND 4
            AND "CurrentValue" BETWEEN 1 AND 4)
    )
)
OR
(
    "Action" = 5
    AND "PreviousValue" IS NULL
    AND "CurrentValue" IS NULL
    AND "RelatedEntityId" IS NOT NULL
    AND "RelatedEntityId" <>
        '00000000-0000-0000-0000-000000000000'::uuid
)
OR
(
    "Action" = 6
    AND "PreviousValue" IN (0, 1)
    AND "CurrentValue" IN (0, 1)
    AND "PreviousValue" <> "CurrentValue"
    AND "RelatedEntityId" IS NULL
)
OR
(
    "Action" = 7
    AND "PreviousValue" BETWEEN 1 AND 4
    AND "CurrentValue" BETWEEN 1 AND 4
    AND "PreviousValue" <> "CurrentValue"
    AND "RelatedEntityId" IS NULL
)
OR
(
    "Action" = 8
    AND "PreviousValue" BETWEEN 1 AND 255
    AND "CurrentValue" BETWEEN 1 AND 255
    AND "PreviousValue" <> "CurrentValue"
    AND "RelatedEntityId" IS NULL
)
OR
(
    "Action" = 9
    AND "PreviousValue" IS NULL
    AND "CurrentValue" >= 1
    AND "RelatedEntityId" IS NULL
)
OR
(
    "Action" = 10
    AND "PreviousValue" IS NULL
    AND "CurrentValue" >= 0
    AND "RelatedEntityId" IS NULL
)
OR
(
    "Action" = 11
    AND "PreviousValue" IS NULL
    AND "CurrentValue" <> 0
    AND "RelatedEntityId" IS NOT NULL
    AND "RelatedEntityId" <>
        '00000000-0000-0000-0000-000000000000'::uuid
)
OR
(
    "Action" = 12
    AND "PreviousValue" IS NULL
    AND "CurrentValue" BETWEEN 1 AND 20
    AND "RelatedEntityId" IS NOT NULL
    AND "RelatedEntityId" <>
        '00000000-0000-0000-0000-000000000000'::uuid
)
""" + ");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM audit.entries WHERE \"Action\" = 12;");

            migrationBuilder.Sql("ALTER TABLE audit.entries DROP CONSTRAINT \"CK_audit_entries_Action\";");
            migrationBuilder.Sql("ALTER TABLE audit.entries DROP CONSTRAINT \"CK_audit_entries_Payload\";");

            migrationBuilder.Sql("ALTER TABLE audit.entries ADD CONSTRAINT \"CK_audit_entries_Action\" CHECK (\"Action\" BETWEEN 1 AND 11);");
            migrationBuilder.Sql(
                "ALTER TABLE audit.entries ADD CONSTRAINT \"CK_audit_entries_Payload\" CHECK (" + """
(
    "Action" BETWEEN 1 AND 4
    AND "PreviousValue" IS NOT NULL
    AND "CurrentValue" IS NOT NULL
    AND "PreviousValue" <> "CurrentValue"
    AND "RelatedEntityId" IS NULL
    AND (
        ("Action" = 1
            AND "PreviousValue" BETWEEN 1 AND 8
            AND "CurrentValue" BETWEEN 1 AND 8)
        OR ("Action" = 2
            AND "PreviousValue" BETWEEN 1 AND 6
            AND "CurrentValue" BETWEEN 1 AND 6)
        OR ("Action" IN (3, 4)
            AND "PreviousValue" BETWEEN 1 AND 4
            AND "CurrentValue" BETWEEN 1 AND 4)
    )
)
OR
(
    "Action" = 5
    AND "PreviousValue" IS NULL
    AND "CurrentValue" IS NULL
    AND "RelatedEntityId" IS NOT NULL
    AND "RelatedEntityId" <>
        '00000000-0000-0000-0000-000000000000'::uuid
)
OR
(
    "Action" = 6
    AND "PreviousValue" IN (0, 1)
    AND "CurrentValue" IN (0, 1)
    AND "PreviousValue" <> "CurrentValue"
    AND "RelatedEntityId" IS NULL
)
OR
(
    "Action" = 7
    AND "PreviousValue" BETWEEN 1 AND 4
    AND "CurrentValue" BETWEEN 1 AND 4
    AND "PreviousValue" <> "CurrentValue"
    AND "RelatedEntityId" IS NULL
)
OR
(
    "Action" = 8
    AND "PreviousValue" BETWEEN 1 AND 127
    AND "CurrentValue" BETWEEN 1 AND 127
    AND "PreviousValue" <> "CurrentValue"
    AND "RelatedEntityId" IS NULL
)
OR
(
    "Action" = 9
    AND "PreviousValue" IS NULL
    AND "CurrentValue" >= 1
    AND "RelatedEntityId" IS NULL
)
OR
(
    "Action" = 10
    AND "PreviousValue" IS NULL
    AND "CurrentValue" >= 0
    AND "RelatedEntityId" IS NULL
)
OR
(
    "Action" = 11
    AND "PreviousValue" IS NULL
    AND "CurrentValue" <> 0
    AND "RelatedEntityId" IS NOT NULL
    AND "RelatedEntityId" <>
        '00000000-0000-0000-0000-000000000000'::uuid
)
""" + ");");

            migrationBuilder.DropTable(
                name: "attendance_alerts",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "attendance_records",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "leave_requests",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "payroll_entries",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "staff_profiles",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "staff_requests",
                schema: "hr");
        }
    }
}
