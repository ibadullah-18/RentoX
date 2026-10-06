using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Notifications;
using RentoX.Infrastructure.Identity;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class PushDeviceConfiguration
    : IEntityTypeConfiguration<PushDevice>
{
    public void Configure(EntityTypeBuilder<PushDevice> builder)
    {
        builder.ToTable("push_devices", "notifications");
        builder.HasKey(device => device.Id);
        builder.Property(device => device.Platform)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(device => device.DeviceId)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(device => device.Token)
            .HasMaxLength(2048)
            .IsRequired();
        builder.Property(device => device.RegisteredAtUtc).IsRequired();
        builder.Property(device => device.LastSeenAtUtc).IsRequired();
        builder.HasIndex(device => device.Token).IsUnique();
        builder.HasIndex(device => new
        {
            device.UserId,
            device.DeviceId,
            device.IsActive
        });
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(device => device.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
