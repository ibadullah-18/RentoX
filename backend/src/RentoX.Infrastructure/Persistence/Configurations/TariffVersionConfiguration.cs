using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Listings.Billing;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class TariffVersionConfiguration : IEntityTypeConfiguration<TariffVersion>
{
    public void Configure(EntityTypeBuilder<TariffVersion> builder)
    {
        builder.ToTable("tariffs", "payments");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.FreeActiveListingLimit).IsRequired();
        builder.Property(t => t.ActivationFee).HasPrecision(18, 2).IsRequired();
        builder.Property(t => t.BumpPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(t => t.VipPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(t => t.VipDurationDays).IsRequired();
        builder.Property(t => t.CreatedByUserId).IsRequired();
        builder.Property(t => t.CreatedAtUtc).IsRequired();

        builder.HasIndex(t => t.CreatedAtUtc);
    }
}
