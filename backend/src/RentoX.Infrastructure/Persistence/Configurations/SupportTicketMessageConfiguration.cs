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