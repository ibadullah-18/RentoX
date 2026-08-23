using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Stores;
using RentoX.Infrastructure.Identity;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class StoreProfileConfiguration
    : IEntityTypeConfiguration<StoreProfile>
{
    public void Configure(
        EntityTypeBuilder<StoreProfile> builder)
    {
        builder.ToTable(
            "store_profiles",
            "stores");

        builder.HasKey(store => store.Id);

        builder.Property(store => store.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(store => store.Slug)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(store => store.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(store => store.PhoneNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(store => store.Email)
            .HasMaxLength(254);

        builder.Property(store => store.Address)
            .HasMaxLength(500);

        builder.Property(store => store.LogoImageKey)
            .HasMaxLength(500);

        builder.Property(store => store.CoverImageKey)
            .HasMaxLength(500);

        builder.Property(store => store.InstagramUrl)
            .HasMaxLength(300);

        builder.Property(store => store.TiktokUrl)
            .HasMaxLength(300);

        builder.Property(store => store.FacebookUrl)
            .HasMaxLength(300);

        builder.Property(store => store.WebsiteUrl)
            .HasMaxLength(300);

        builder.Property(store => store.RejectionReason)
            .HasMaxLength(1000);

        builder.Property(store => store.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.HasIndex(store => store.OwnerId)
            .IsUnique();

        builder.HasIndex(store => store.Slug)
            .IsUnique();

        builder.HasIndex(store => new
        {
            store.Status,
            store.CreatedAtUtc
        });

        builder.HasOne<AppUser>()
            .WithOne()
            .HasForeignKey<StoreProfile>(
                store => store.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(store =>
            store.DeletedAtUtc == null);

        builder.Ignore(store => store.DomainEvents);
    }
}