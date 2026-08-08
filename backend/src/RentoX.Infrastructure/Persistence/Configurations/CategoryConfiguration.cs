using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Catalog.Categories;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration
    : IEntityTypeConfiguration<Category>
{
    public void Configure(
        EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories", "catalog");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Slug)
            .HasMaxLength(160)
            .IsRequired();

        builder.HasIndex(category => category.Slug)
            .IsUnique();

        builder.Property(category => category.IconUrl)
            .HasMaxLength(500);

        builder.Property(category => category.DisplayOrder)
            .IsRequired();

        builder.Property(category => category.IsActive)
            .IsRequired();

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(category => category.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(category => category.Translations)
            .WithOne()
            .HasForeignKey(translation =>
                translation.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}