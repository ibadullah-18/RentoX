using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Listings;
using RentoX.Infrastructure.Identity;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class ListingViewConfiguration
    : IEntityTypeConfiguration<ListingView>
{
    public void Configure(
        EntityTypeBuilder<ListingView> builder)
    {
        builder.ToTable(
            "listing_views",
            "listings");

        builder.HasKey(view => view.Id);

        builder.Property(view => view.ViewedAtUtc)
            .IsRequired();

        builder.HasIndex(view => new
        {
            view.ListingId,
            view.ViewerUserId
        }).IsUnique();

        builder.HasIndex(view =>
            view.ViewerUserId);

        builder.HasOne<Listing>()
            .WithMany()
            .HasForeignKey(view => view.ListingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(view => view.ViewerUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}