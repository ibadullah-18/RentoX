using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Stores;
using RentoX.Domain.Wallets;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class StoreVipPlanConfiguration : IEntityTypeConfiguration<StoreVipPlan>
{
    public void Configure(EntityTypeBuilder<StoreVipPlan> builder)
    {
        builder.ToTable("store_vip_plans", "stores");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(StoreVipPlan.MaximumNameLength).IsRequired();
        builder.Property(p => p.Price).HasPrecision(18, 2).IsRequired();
        builder.Property(p => p.OfferMessage).HasMaxLength(StoreVipPlan.MaximumMessageLength).IsRequired();
        builder.HasIndex(p => p.CreatedAtUtc);
    }
}

public sealed class StoreVipPurchaseConfiguration : IEntityTypeConfiguration<StoreVipPurchase>
{
    public void Configure(EntityTypeBuilder<StoreVipPurchase> builder)
    {
        builder.ToTable("store_vip_purchases", "stores");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.PlanName).HasMaxLength(StoreVipPlan.MaximumNameLength).IsRequired();
        builder.Property(p => p.ChargedAmount).HasPrecision(18, 2).IsRequired();
        builder.HasOne<StoreProfile>().WithMany().HasForeignKey(p => p.StoreId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<WalletTransaction>().WithMany().HasForeignKey(p => p.WalletTransactionId).OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(p => p.IsGift);
        // Several gifts have no payment (null): a unique index lets nulls repeat.
        builder.HasIndex(p => p.WalletTransactionId).IsUnique();
        builder.HasIndex(p => new { p.StoreId, p.EndsAtUtc });
    }
}
