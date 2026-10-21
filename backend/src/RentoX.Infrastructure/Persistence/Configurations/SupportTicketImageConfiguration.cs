using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Support;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class SupportTicketImageConfiguration : IEntityTypeConfiguration<SupportTicketImage>
{
    public void Configure(EntityTypeBuilder<SupportTicketImage> builder)
    {
        builder.ToTable("support_ticket_images", "support", table =>
        {
            table.HasCheckConstraint("CK_support_ticket_images_SizeBytes", "\"SizeBytes\" > 0 AND \"SizeBytes\" <= 10485760");
            table.HasCheckConstraint("CK_support_ticket_images_DisplayOrder", "\"DisplayOrder\" >= 0 AND \"DisplayOrder\" < 5");
        });

        builder.HasKey(image => image.Id);
        builder.Property(image => image.StorageKey).HasMaxLength(500).IsRequired();
        builder.Property(image => image.ContentType).HasMaxLength(32).IsRequired();
        builder.HasIndex(image => new { image.MessageId, image.DisplayOrder }).IsUnique();
    }
}
