using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Promotions;
using RentoX.Domain.Wallets;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class ListingPromotionConfiguration
    : IEntityTypeConfiguration<ListingPromotion>
{
    public void Configure(
        EntityTypeBuilder<ListingPromotion> builder)
    {
        builder.ToTable(
            "listing_promotions",
            "payments");

        builder.HasKey(promotion =>
            promotion.Id);

        builder.Property(promotion =>
                promotion.ListingId)
            .IsRequired();

        builder.Property(promotion =>
                promotion.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(promotion =>
                promotion.ChargedAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(promotion =>
                promotion.WalletTransactionId)
            .IsRequired();

        builder.Property(promotion =>
                promotion.StartsAtUtc)
            .IsRequired();

        builder.Property(promotion =>
                promotion.PurchasedAtUtc)
            .IsRequired();

        builder.Ignore(promotion =>
            promotion.IsBump);

        builder.HasIndex(promotion =>
                promotion.WalletTransactionId)
            .IsUnique();

        builder.HasIndex(promotion => new
        {
            promotion.ListingId,
            promotion.Type,
            promotion.StartsAtUtc
        });

        builder.HasIndex(promotion => new
        {
            promotion.ListingId,
            promotion.Type,
            promotion.EndsAtUtc
        });

        builder.HasOne<Listing>()
            .WithMany()
            .HasForeignKey(promotion =>
                promotion.ListingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<WalletTransaction>()
            .WithMany()
            .HasForeignKey(promotion =>
                promotion.WalletTransactionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
