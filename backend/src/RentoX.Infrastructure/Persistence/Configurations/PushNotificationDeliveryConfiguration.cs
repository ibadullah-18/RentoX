using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Notifications;
using RentoX.Infrastructure.Identity;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class PushNotificationDeliveryConfiguration
    : IEntityTypeConfiguration<PushNotificationDelivery>
{
    public void Configure(EntityTypeBuilder<PushNotificationDelivery> builder)
    {
        builder.ToTable("push_deliveries", "notifications");
        builder.HasKey(delivery => delivery.Id);
        builder.Property(delivery => delivery.Status)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(delivery => delivery.LastError)
            .HasMaxLength(2000);
        builder.HasIndex(delivery => delivery.NotificationId).IsUnique();
        builder.HasIndex(delivery => new
        {
            delivery.Status,
            delivery.NextAttemptAtUtc
        });
        builder.HasOne<Notification>()
            .WithMany()
            .HasForeignKey(delivery => delivery.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(delivery => delivery.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
