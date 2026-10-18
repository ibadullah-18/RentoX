using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentoX.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFreeServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FreeServices",
                schema: "users",
                table: "user_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "FreeServicesGrantedByUserId",
                schema: "users",
                table: "user_profiles",
                type: "uuid",
                nullable: true);

            // Audit log: gift entries also say that a person's services were switched free on (4) or off (5).
            migrationBuilder.Sql("ALTER TABLE audit.entries DROP CONSTRAINT \"CK_audit_entries_Payload\";");
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
OR
(
    "Action" = 13
    AND "PreviousValue" IS NULL
    AND "CurrentValue" BETWEEN 1 AND 5
    AND "RelatedEntityId" IS NOT NULL
    AND "RelatedEntityId" <>
        '00000000-0000-0000-0000-000000000000'::uuid
)
""" + ");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM audit.entries WHERE \"Action\" = 13 AND \"CurrentValue\" IN (4, 5);");
            migrationBuilder.Sql("ALTER TABLE audit.entries DROP CONSTRAINT \"CK_audit_entries_Payload\";");
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
OR
(
    "Action" = 13
    AND "PreviousValue" IS NULL
    AND "CurrentValue" BETWEEN 1 AND 3
    AND "RelatedEntityId" IS NOT NULL
    AND "RelatedEntityId" <>
        '00000000-0000-0000-0000-000000000000'::uuid
)
""" + ");");

            migrationBuilder.DropColumn(
                name: "FreeServices",
                schema: "users",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "FreeServicesGrantedByUserId",
                schema: "users",
                table: "user_profiles");
        }
    }
}
