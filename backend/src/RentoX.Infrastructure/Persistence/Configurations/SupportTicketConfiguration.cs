using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Support;
using RentoX.Infrastructure.Identity;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class SupportTicketConfiguration
    : IEntityTypeConfiguration<SupportTicket>
{
    public void Configure(
        EntityTypeBuilder<SupportTicket> builder)
    {
        builder.ToTable(
            "support_tickets",
            "support",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_support_tickets_Category",
                    "\"Category\" >= 1 AND \"Category\" <= 7");

                tableBuilder.HasCheckConstraint(
                    "CK_support_tickets_Priority",
                    "\"Priority\" >= 1 AND \"Priority\" <= 4");

                tableBuilder.HasCheckConstraint(
                    "CK_support_tickets_Status",
                    "\"Status\" >= 1 AND \"Status\" <= 4");
            });

        builder.HasKey(ticket => ticket.Id);

        builder.Property(ticket => ticket.Category)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(ticket => ticket.Subject)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(ticket => ticket.Priority)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(ticket => ticket.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(ticket => ticket.CreatedAtUtc)
            .IsRequired();

        builder.Property(ticket => ticket.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(ticket => new
        {
            ticket.UserId,
            ticket.UpdatedAtUtc
        });

        builder.HasIndex(ticket => new
        {
            ticket.Status,
            ticket.Priority,
            ticket.UpdatedAtUtc
        });

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(ticket => ticket.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(ticket => ticket.Messages)
            .WithOne()
            .HasForeignKey(message =>
                message.SupportTicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(ticket => ticket.Messages)
            .UsePropertyAccessMode(
                PropertyAccessMode.Field);
    }
}