using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Support;
using RentoX.Infrastructure.Identity;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class SupportTicketMessageConfiguration
    : IEntityTypeConfiguration<SupportTicketMessage>
{
    public void Configure(
        EntityTypeBuilder<SupportTicketMessage> builder)
    {
        builder.ToTable(
            "support_ticket_messages",
            "support");

        builder.HasKey(message => message.Id);

        builder.HasMany(message => message.Images)
            .WithOne()
            .HasForeignKey(image => image.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(message => message.Images)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(message => message.Body)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(message => message.SentAtUtc)
            .IsRequired();

        builder.HasIndex(message => new
        {
            message.SupportTicketId,
            message.SentAtUtc,
            message.Id
        });

        builder.HasIndex(message =>
            message.SenderId);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(message =>
                message.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}