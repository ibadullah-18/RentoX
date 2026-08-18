using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Listings;
using RentoX.Infrastructure.Identity;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class ListingConfiguration
    : IEntityTypeConfiguration<Listing>
{
    public void Configure(
        EntityTypeBuilder<Listing> builder)
    {
        builder.ToTable("listings", "listings");

        builder.HasKey(listing => listing.Id);

        builder.Property(listing => listing.Title)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(listing => listing.Description)
            .HasMaxLength(10_000)
            .IsRequired();

        builder.Property(listing => listing.Price)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(listing => listing.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(listing => listing.Status)
            .IsRequired();

        builder.Property(listing =>
                listing.RentalPeriodUnit)
            .IsRequired();

        builder.Property(listing =>
                listing.RejectionReason)
            .HasMaxLength(1000);

        builder.HasIndex(listing => listing.OwnerId);

        builder.HasIndex(listing => new
        {
            listing.CategoryId,
            listing.Status,
            listing.PublishedAtUtc
        });

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(listing => listing.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(listing => listing.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(listing => listing.Images)
            .WithOne()
            .HasForeignKey(image => image.ListingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(listing => listing.Images)
            .UsePropertyAccessMode(
                PropertyAccessMode.Field);

        builder.HasMany(listing =>
        listing.FieldValues)
            .WithOne()
            .HasForeignKey(value => value.ListingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(listing =>
                listing.FieldValues)
            .UsePropertyAccessMode(
                PropertyAccessMode.Field);
    }
}