using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Catalog.Fields;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class CategoryFieldConfiguration
    : IEntityTypeConfiguration<CategoryField>
{
    public void Configure(
        EntityTypeBuilder<CategoryField> builder)
    {
        builder.ToTable("category_fields", "catalog");

        builder.HasKey(field => field.Id);

        builder.Property(field => field.Key)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(field => new
        {
            field.CategoryId,
            field.Key
        }).IsUnique();

        builder.HasOne<
                RentoX.Domain.Catalog.Categories.Category>()
            .WithMany()
            .HasForeignKey(field => field.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(field => field.Translations)
            .WithOne()
            .HasForeignKey(translation =>
                translation.CategoryFieldId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(field => field.Options)
            .WithOne()
            .HasForeignKey(option =>
                option.CategoryFieldId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}