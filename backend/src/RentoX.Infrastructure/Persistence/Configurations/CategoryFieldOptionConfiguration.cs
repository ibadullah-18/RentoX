using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Catalog.Fields;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class CategoryFieldOptionConfiguration
    : IEntityTypeConfiguration<CategoryFieldOption>
{
    public void Configure(
        EntityTypeBuilder<CategoryFieldOption> builder)
    {
        builder.ToTable(
            "category_field_options",
            "catalog");

        builder.HasKey(option => option.Id);

        builder.Property(option => option.Value)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(option => new
        {
            option.CategoryFieldId,
            option.Value
        }).IsUnique();

        builder.HasMany(option => option.Translations)
            .WithOne()
            .HasForeignKey(translation =>
                translation.CategoryFieldOptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}