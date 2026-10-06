using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Notifications;
using RentoX.Infrastructure.Identity;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration
    : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications", "notifications");
        builder.HasKey(notification => notification.Id);
        builder.Property(notification => notification.Type)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(notification => notification.Title)
            .HasMaxLength(160)
            .IsRequired();
        builder.Property(notification => notification.Body)
            .HasMaxLength(1000)
            .IsRequired();
        builder.Property(notification => notification.ActionUrl)
            .HasMaxLength(500);
        builder.Property(notification => notification.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(notification => new
        {
            notification.UserId,
            notification.CreatedAtUtc
        });
        builder.HasIndex(notification => new
        {
            notification.UserId,
            notification.ReadAtUtc
        });

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(notification => notification.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
