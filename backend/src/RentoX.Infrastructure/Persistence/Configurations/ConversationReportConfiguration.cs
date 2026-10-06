using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Messaging;
using RentoX.Infrastructure.Identity;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class ConversationReportConfiguration
    : IEntityTypeConfiguration<ConversationReport>
{
    public void Configure(EntityTypeBuilder<ConversationReport> builder)
    {
        builder.ToTable("conversation_reports", "messaging");
        builder.HasKey(report => report.Id);
        builder.Property(report => report.Reason).HasConversion<int>().IsRequired();
        builder.Property(report => report.Status).HasConversion<int>().IsRequired();
        builder.Property(report => report.Details).HasMaxLength(1000);
        builder.Property(report => report.ResolutionNote).HasMaxLength(1000);
        builder.Property(report => report.CreatedAtUtc).IsRequired();

        builder.HasIndex(report => new { report.Status, report.CreatedAtUtc });
        builder.HasIndex(report => new { report.ConversationId, report.ReporterId })
            .IsUnique()
            .HasFilter("\"Status\" IN (1, 2)");

        builder.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(report => report.ConversationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Message>()
            .WithMany()
            .HasForeignKey(report => report.EvidenceMessageId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(report => report.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(report => report.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
