using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Catalog.Fields;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class CategoryFieldOptionTranslationConfiguration
    : IEntityTypeConfiguration<
        CategoryFieldOptionTranslation>
{
    public void Configure(
        EntityTypeBuilder<
            CategoryFieldOptionTranslation> builder)
    {
        builder.ToTable(
            "category_field_option_translations",
            "catalog");

        builder.HasKey(translation => translation.Id);

        builder.Property(translation => translation.Label)
            .HasMaxLength(120)
            .IsRequired();

        builder.HasIndex(translation => new
        {
            translation.CategoryFieldOptionId,
            translation.Language
        }).IsUnique();
    }
}