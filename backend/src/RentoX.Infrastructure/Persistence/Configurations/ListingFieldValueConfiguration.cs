using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Catalog.Fields;
using RentoX.Domain.Listings;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class ListingFieldValueConfiguration
    : IEntityTypeConfiguration<ListingFieldValue>
{
    public void Configure(
        EntityTypeBuilder<ListingFieldValue> builder)
    {
        builder.ToTable(
            "listing_field_values",
            "listings");

        builder.HasKey(value => value.Id);

        builder.Property(value => value.TextValue)
            .HasMaxLength(2000);

        builder.Property(value => value.NumericValue)
            .HasPrecision(18, 4);

        builder.Property(value => value.CustomValue)
            .HasMaxLength(500);

        builder.HasIndex(value => new
        {
            value.ListingId,
            value.CategoryFieldId
        }).IsUnique();

        builder.HasIndex(value => new
        {
            value.CategoryFieldId,
            value.NumericValue
        });

        builder.HasIndex(value => new
        {
            value.CategoryFieldId,
            value.FlagValue
        });

        builder.HasOne<CategoryField>()
            .WithMany()
            .HasForeignKey(value =>
                value.CategoryFieldId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(value => value.Selections)
            .WithOne()
            .HasForeignKey(selection =>
                selection.ListingFieldValueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(value => value.Selections)
            .UsePropertyAccessMode(
                PropertyAccessMode.Field);
    }
}