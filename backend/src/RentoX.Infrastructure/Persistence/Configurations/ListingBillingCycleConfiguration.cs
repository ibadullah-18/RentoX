using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Billing;
using RentoX.Domain.Wallets;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class ListingBillingCycleConfiguration
    : IEntityTypeConfiguration<ListingBillingCycle>
{
    public void Configure(
        EntityTypeBuilder<ListingBillingCycle> builder)
    {
        builder.ToTable(
            "listing_billing_cycles",
            "payments");

        builder.HasKey(cycle => cycle.Id);

        builder.Property(cycle => cycle.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(cycle =>
                cycle.ChargedAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(cycle =>
                cycle.PeriodStartUtc)
            .IsRequired();

        builder.Property(cycle =>
                cycle.PeriodEndUtc)
            .IsRequired();

        builder.Property(cycle =>
                cycle.CreatedAtUtc)
            .IsRequired();

        builder.Ignore(cycle => cycle.IsFree);
        builder.Ignore(cycle => cycle.IsRefunded);

        builder.HasIndex(cycle => new
        {
            cycle.ListingId,
            cycle.CycleNumber
        }).IsUnique();

        builder.HasIndex(cycle =>
                cycle.WalletTransactionId)
            .IsUnique();

        builder.HasIndex(cycle =>
                cycle.RefundWalletTransactionId)
            .IsUnique();

        builder.HasIndex(cycle => new
        {
            cycle.ListingId,
            cycle.PeriodEndUtc
        });

        builder.HasOne<Listing>()
            .WithMany()
            .HasForeignKey(cycle =>
                cycle.ListingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<WalletTransaction>()
            .WithMany()
            .HasForeignKey(cycle =>
                cycle.WalletTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<WalletTransaction>()
            .WithMany()
            .HasForeignKey(cycle =>
                cycle.RefundWalletTransactionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
