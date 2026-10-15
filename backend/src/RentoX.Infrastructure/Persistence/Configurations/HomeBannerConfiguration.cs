using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Content;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class HomeBannerConfiguration : IEntityTypeConfiguration<HomeBanner>
{
    public void Configure(EntityTypeBuilder<HomeBanner> builder)
    {
        builder.ToTable("home_banners", "content");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.TitleAz).HasMaxLength(HomeBanner.MaximumTitleLength).IsRequired();
        builder.Property(b => b.TitleRu).HasMaxLength(HomeBanner.MaximumTitleLength).IsRequired();
        builder.Property(b => b.TitleEn).HasMaxLength(HomeBanner.MaximumTitleLength).IsRequired();
        builder.Property(b => b.SubtitleAz).HasMaxLength(HomeBanner.MaximumSubtitleLength);
        builder.Property(b => b.SubtitleRu).HasMaxLength(HomeBanner.MaximumSubtitleLength);
        builder.Property(b => b.SubtitleEn).HasMaxLength(HomeBanner.MaximumSubtitleLength);
        builder.Property(b => b.LinkUrl).HasMaxLength(HomeBanner.MaximumLinkLength);
        builder.Property(b => b.ImageKey).HasMaxLength(300);
        builder.Property(b => b.DisplayOrder).IsRequired();
        builder.Property(b => b.IsActive).IsRequired();
        builder.Property(b => b.CreatedByUserId).IsRequired();
        builder.Property(b => b.CreatedAtUtc).IsRequired();

        builder.HasIndex(b => new { b.IsActive, b.DisplayOrder });
    }
}
