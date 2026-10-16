using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Stores;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class StoreDayStatConfiguration : IEntityTypeConfiguration<StoreDayStat>
{
    public void Configure(EntityTypeBuilder<StoreDayStat> builder)
    {
        builder.ToTable("store_day_stats", "stores");
        builder.HasKey(s => new { s.StoreId, s.Day });
        builder.HasOne<StoreProfile>().WithMany().HasForeignKey(s => s.StoreId).OnDelete(DeleteBehavior.Cascade);
    }
}
