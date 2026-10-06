using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Auditing;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class AuditLogEntryConfiguration
    : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable(
            "entries",
            "audit",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_audit_entries_Action",
                    "\"Action\" BETWEEN 1 AND 5");

                table.HasCheckConstraint(
                    "CK_audit_entries_RequiredIds",
                    """
                    "OperationId" <> '00000000-0000-0000-0000-000000000000'::uuid
                    AND "ActorUserId" <> '00000000-0000-0000-0000-000000000000'::uuid
                    AND "TargetId" <> '00000000-0000-0000-0000-000000000000'::uuid
                    """);

                table.HasCheckConstraint(
                    "CK_audit_entries_Payload",
                    """
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
                    """);
            });

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Action)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(entry => entry.OccurredAtUtc)
            .IsRequired();

        builder.HasIndex(entry => new
        {
            entry.OperationId,
            entry.Action,
            entry.TargetId
        }).IsUnique();

        builder.HasIndex(entry => new
        {
            entry.OccurredAtUtc,
            entry.Id
        });

        builder.HasIndex(entry => new
        {
            entry.ActorUserId,
            entry.OccurredAtUtc,
            entry.Id
        });

        builder.HasIndex(entry => new
        {
            entry.TargetId,
            entry.Action,
            entry.OccurredAtUtc,
            entry.Id
        });

        // Deliberately no foreign keys to users or business entities:
        // deleting a user, ticket or listing must not erase audit history.
    }
}