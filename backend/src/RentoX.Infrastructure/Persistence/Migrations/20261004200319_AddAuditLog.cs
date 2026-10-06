using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentoX.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.CreateTable(
                name: "entries",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    PreviousValue = table.Column<int>(type: "integer", nullable: true),
                    CurrentValue = table.Column<int>(type: "integer", nullable: true),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entries", x => x.Id);
                    table.CheckConstraint("CK_audit_entries_Action", "\"Action\" BETWEEN 1 AND 5");
                    table.CheckConstraint("CK_audit_entries_Payload", "(\n    \"Action\" BETWEEN 1 AND 4\n    AND \"PreviousValue\" IS NOT NULL\n    AND \"CurrentValue\" IS NOT NULL\n    AND \"PreviousValue\" <> \"CurrentValue\"\n    AND \"RelatedEntityId\" IS NULL\n    AND (\n        (\"Action\" = 1\n            AND \"PreviousValue\" BETWEEN 1 AND 8\n            AND \"CurrentValue\" BETWEEN 1 AND 8)\n        OR (\"Action\" = 2\n            AND \"PreviousValue\" BETWEEN 1 AND 6\n            AND \"CurrentValue\" BETWEEN 1 AND 6)\n        OR (\"Action\" IN (3, 4)\n            AND \"PreviousValue\" BETWEEN 1 AND 4\n            AND \"CurrentValue\" BETWEEN 1 AND 4)\n    )\n)\nOR\n(\n    \"Action\" = 5\n    AND \"PreviousValue\" IS NULL\n    AND \"CurrentValue\" IS NULL\n    AND \"RelatedEntityId\" IS NOT NULL\n    AND \"RelatedEntityId\" <>\n        '00000000-0000-0000-0000-000000000000'::uuid\n)");
                    table.CheckConstraint("CK_audit_entries_RequiredIds", "\"OperationId\" <> '00000000-0000-0000-0000-000000000000'::uuid\nAND \"ActorUserId\" <> '00000000-0000-0000-0000-000000000000'::uuid\nAND \"TargetId\" <> '00000000-0000-0000-0000-000000000000'::uuid");
                });

            migrationBuilder.CreateIndex(
                name: "IX_entries_ActorUserId_OccurredAtUtc_Id",
                schema: "audit",
                table: "entries",
                columns: new[] { "ActorUserId", "OccurredAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_entries_OccurredAtUtc_Id",
                schema: "audit",
                table: "entries",
                columns: new[] { "OccurredAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_entries_OperationId_Action_TargetId",
                schema: "audit",
                table: "entries",
                columns: new[] { "OperationId", "Action", "TargetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_entries_TargetId_Action_OccurredAtUtc_Id",
                schema: "audit",
                table: "entries",
                columns: new[] { "TargetId", "Action", "OccurredAtUtc", "Id" });

            migrationBuilder.Sql(
                """
                CREATE FUNCTION audit.reject_entry_mutation()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $audit$
                BEGIN
                    RAISE EXCEPTION 'Audit entries are append-only.'
                        USING ERRCODE = '55000';
                END;
                $audit$;

                CREATE TRIGGER audit_entries_immutable
                BEFORE UPDATE OR DELETE OR TRUNCATE
                ON audit.entries
                FOR EACH STATEMENT
                EXECUTE FUNCTION audit.reject_entry_mutation();

                ALTER TABLE audit.entries
                    ENABLE ALWAYS TRIGGER audit_entries_immutable;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                LOCK TABLE audit.entries IN ACCESS EXCLUSIVE MODE;

                DO $audit$
                BEGIN
                    IF EXISTS (SELECT 1 FROM audit.entries) THEN
                        RAISE EXCEPTION
                            'Cannot remove an audit table containing records.'
                            USING ERRCODE = '55000';
                    END IF;
                END;
                $audit$;
                """);

            migrationBuilder.DropTable(
                name: "entries",
                schema: "audit");

            migrationBuilder.Sql(
                "DROP FUNCTION audit.reject_entry_mutation();");
        }
    }
}
