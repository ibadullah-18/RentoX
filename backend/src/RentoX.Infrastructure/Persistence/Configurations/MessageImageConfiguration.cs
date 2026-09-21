using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Messaging;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class MessageImageConfiguration : IEntityTypeConfiguration<MessageImage>
{
    public void Configure(EntityTypeBuilder<MessageImage> builder)
    {
        builder.ToTable("message_images", "messaging", table =>
        {
            table.HasCheckConstraint("CK_message_images_SizeBytes",
                "\"SizeBytes\" > 0 AND \"SizeBytes\" <= 10485760");
            table.HasCheckConstraint("CK_message_images_DisplayOrder",
                "\"DisplayOrder\" >= 0 AND \"DisplayOrder\" < 5");
        });
        builder.HasKey(image => image.Id);
        builder.Property(image => image.StorageKey).HasMaxLength(500).IsRequired();
        builder.Property(image => image.ContentType).HasMaxLength(32).IsRequired();
        builder.HasIndex(image => new { image.MessageId, image.DisplayOrder }).IsUnique();
        builder.HasOne<Message>()
            .WithMany(message => message.Images)
            .HasForeignKey(image => image.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
