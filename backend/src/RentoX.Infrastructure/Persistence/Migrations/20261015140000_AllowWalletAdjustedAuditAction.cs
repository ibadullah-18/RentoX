using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentoX.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// The audit log also records money a staff member adds to or takes from a wallet
    /// by hand (action 11; the current value is the signed amount in qəpik, the related
    /// id is the wallet transaction). Only the two check constraints change.
    /// </summary>
    [DbContext(typeof(RentoXDbContext))]
    [Migration("20261015140000_AllowWalletAdjustedAuditAction")]
    public partial class AllowWalletAdjustedAuditAction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM audit.entries WHERE \"Action\" = 11;");

            migrationBuilder.Sql("ALTER TABLE audit.entries DROP CONSTRAINT \"CK_audit_entries_Action\";");
            migrationBuilder.Sql("ALTER TABLE audit.entries DROP CONSTRAINT \"CK_audit_entries_Payload\";");

            migrationBuilder.Sql("ALTER TABLE audit.entries ADD CONSTRAINT \"CK_audit_entries_Action\" CHECK (\"Action\" BETWEEN 1 AND 10);");
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
""" + ");");
        }
    }
}
