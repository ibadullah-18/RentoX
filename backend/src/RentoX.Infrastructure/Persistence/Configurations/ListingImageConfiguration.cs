using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Listings;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class ListingImageConfiguration
    : IEntityTypeConfiguration<ListingImage>
{
    public void Configure(
        EntityTypeBuilder<ListingImage> builder)
    {
        builder.ToTable(
            "listing_images",
            "listings");

        builder.HasKey(image => image.Id);

        builder.Property(image => image.StorageKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(image => image.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(image => image.SizeBytes)
            .IsRequired();

        builder.Property(image => image.DisplayOrder)
            .IsRequired();

        builder.Property(image => image.IsCover)
            .IsRequired();

        builder.HasIndex(image => new
        {
            image.ListingId,
            image.DisplayOrder
        });

        builder.HasIndex(image => image.StorageKey)
            .IsUnique();
    }
}