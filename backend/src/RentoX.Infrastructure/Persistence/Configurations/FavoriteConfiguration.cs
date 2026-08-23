using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Favorites;
using RentoX.Domain.Listings;
using RentoX.Infrastructure.Identity;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class FavoriteConfiguration
    : IEntityTypeConfiguration<Favorite>
{
    public void Configure(
        EntityTypeBuilder<Favorite> builder)
    {
        builder.ToTable(
            "favorites",
            "engagement");

        builder.HasKey(favorite => favorite.Id);

        builder.Property(favorite =>
                favorite.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(favorite => new
        {
            favorite.UserId,
            favorite.ListingId
        }).IsUnique();

        builder.HasIndex(favorite => new
        {
            favorite.UserId,
            favorite.CreatedAtUtc
        });

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(favorite =>
                favorite.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Listing>()
            .WithMany()
            .HasForeignKey(favorite =>
                favorite.ListingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(favorite =>
            favorite.ListingId);
    }
}