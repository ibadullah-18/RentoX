using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentoX.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Blocking an account: a reason column on the profile, and the audit log
    /// may record the status change (action 7).
    /// </summary>
    [DbContext(typeof(RentoXDbContext))]
    [Migration("20261014130000_AddUserBlocking")]
    public partial class AddUserBlocking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StatusReason",
                schema: "users",
                table: "user_profiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.Sql("ALTER TABLE audit.entries DROP CONSTRAINT \"CK_audit_entries_Action\";");
            migrationBuilder.Sql("ALTER TABLE audit.entries DROP CONSTRAINT \"CK_audit_entries_Payload\";");

            migrationBuilder.Sql("ALTER TABLE audit.entries ADD CONSTRAINT \"CK_audit_entries_Action\" CHECK (\"Action\" BETWEEN 1 AND 7);");
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
""" + ");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM audit.entries WHERE \"Action\" = 7;");

            migrationBuilder.Sql("ALTER TABLE audit.entries DROP CONSTRAINT \"CK_audit_entries_Action\";");
            migrationBuilder.Sql("ALTER TABLE audit.entries DROP CONSTRAINT \"CK_audit_entries_Payload\";");

            migrationBuilder.Sql("ALTER TABLE audit.entries ADD CONSTRAINT \"CK_audit_entries_Action\" CHECK (\"Action\" BETWEEN 1 AND 6);");
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
""" + ");");

            migrationBuilder.DropColumn(
                name: "StatusReason",
                schema: "users",
                table: "user_profiles");
        }
    }
}
