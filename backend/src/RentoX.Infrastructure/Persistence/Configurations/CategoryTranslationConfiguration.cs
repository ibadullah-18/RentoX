using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Catalog.Categories;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class CategoryTranslationConfiguration
    : IEntityTypeConfiguration<CategoryTranslation>
{
    public void Configure(
        EntityTypeBuilder<CategoryTranslation> builder)
    {
        builder.ToTable(
            "category_translations",
            "catalog");

        builder.HasKey(translation => translation.Id);

        builder.Property(translation => translation.Language)
            .IsRequired();

        builder.Property(translation => translation.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.HasIndex(translation => new
        {
            translation.CategoryId,
            translation.Language
        }).IsUnique();
    }
}