using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Locations;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("cities", "locations");
        builder.HasKey(city => city.Id);

        builder.Property(city => city.NameAz).HasMaxLength(100).IsRequired();
        builder.Property(city => city.NameRu).HasMaxLength(100).IsRequired();
        builder.Property(city => city.NameEn).HasMaxLength(100).IsRequired();
        builder.Property(city => city.DisplayOrder).IsRequired();
        builder.Property(city => city.IsActive).IsRequired();

        builder.HasIndex(city => city.NameAz).IsUnique();
    }
}

public sealed class DistrictConfiguration : IEntityTypeConfiguration<District>
{
    public void Configure(EntityTypeBuilder<District> builder)
    {
        builder.ToTable("districts", "locations");
        builder.HasKey(district => district.Id);

        builder.Property(district => district.NameAz).HasMaxLength(100).IsRequired();
        builder.Property(district => district.NameRu).HasMaxLength(100).IsRequired();
        builder.Property(district => district.NameEn).HasMaxLength(100).IsRequired();
        builder.Property(district => district.DisplayOrder).IsRequired();
        builder.Property(district => district.IsActive).IsRequired();

        builder.HasIndex(district => new { district.CityId, district.NameAz })
            .IsUnique();

        builder.HasOne<City>()
            .WithMany()
            .HasForeignKey(district => district.CityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
