using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Moderation;
using RentoX.Infrastructure.Identity;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class ContentReportConfiguration
    : IEntityTypeConfiguration<ContentReport>
{
    public void Configure(EntityTypeBuilder<ContentReport> builder)
    {
        builder.ToTable("content_reports", "moderation");
        builder.HasKey(report => report.Id);

        builder.Property(report => report.TargetType)
            .HasConversion<int>().IsRequired();
        builder.Property(report => report.Reason)
            .HasConversion<int>().IsRequired();
        builder.Property(report => report.Status)
            .HasConversion<int>().IsRequired();
        builder.Property(report => report.Details).HasMaxLength(1000);
        builder.Property(report => report.ResolutionNote).HasMaxLength(1000);
        builder.Property(report => report.CreatedAtUtc).IsRequired();

        builder.HasIndex(report => new { report.Status, report.CreatedAtUtc });
        builder.HasIndex(report => new { report.TargetType, report.TargetId });

        // One open report per person and target.
        builder.HasIndex(report => new
            {
                report.TargetType,
                report.TargetId,
                report.ReporterId
            })
            .IsUnique()
            .HasFilter("\"Status\" IN (1, 2)");

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
