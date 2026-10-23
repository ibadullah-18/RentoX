using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Settings;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class SettingsVersionConfiguration : IEntityTypeConfiguration<SettingsVersion>
{
    public void Configure(EntityTypeBuilder<SettingsVersion> builder)
    {
        builder.ToTable("settings_versions", "admin");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.ListingLifetimeDays).IsRequired();
        builder.Property(s => s.MaxListingPhotos).IsRequired();
        builder.Property(s => s.MinListingPhotos).IsRequired().HasDefaultValue(0);
        builder.Property(s => s.MaxNewListingsPerDay).IsRequired().HasDefaultValue(0);
        builder.Property(s => s.CreatedByUserId).IsRequired();
        builder.Property(s => s.CreatedAtUtc).IsRequired();

        builder.HasIndex(s => s.CreatedAtUtc);
    }
}
