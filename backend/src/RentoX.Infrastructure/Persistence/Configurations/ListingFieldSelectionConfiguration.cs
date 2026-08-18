using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Catalog.Fields;
using RentoX.Domain.Listings;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class ListingFieldSelectionConfiguration
    : IEntityTypeConfiguration<ListingFieldSelection>
{
    public void Configure(
        EntityTypeBuilder<ListingFieldSelection> builder)
    {
        builder.ToTable(
            "listing_field_selections",
            "listings");

        builder.HasKey(selection => selection.Id);

        builder.HasIndex(selection => new
        {
            selection.ListingFieldValueId,
            selection.CategoryFieldOptionId
        }).IsUnique();

        builder.HasIndex(selection =>
            selection.CategoryFieldOptionId);

        builder.HasOne<CategoryFieldOption>()
            .WithMany()
            .HasForeignKey(selection =>
                selection.CategoryFieldOptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}