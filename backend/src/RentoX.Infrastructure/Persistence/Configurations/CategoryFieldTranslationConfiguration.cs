using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Catalog.Fields;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class CategoryFieldTranslationConfiguration
    : IEntityTypeConfiguration<CategoryFieldTranslation>
{
    public void Configure(
        EntityTypeBuilder<CategoryFieldTranslation> builder)
    {
        builder.ToTable(
            "category_field_translations",
            "catalog");

        builder.HasKey(translation => translation.Id);

        builder.Property(translation => translation.Label)
            .HasMaxLength(120)
            .IsRequired();

        builder.HasIndex(translation => new
        {
            translation.CategoryFieldId,
            translation.Language
        }).IsUnique();
    }
}