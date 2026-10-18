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

        builder.Property(b => b.PageSlug).HasMaxLength(HomeBanner.MaximumSlugLength);
        builder.Property(b => b.PageBodyAz).HasMaxLength(HomeBanner.MaximumPageBodyLength);
        builder.Property(b => b.PageBodyRu).HasMaxLength(HomeBanner.MaximumPageBodyLength);
        builder.Property(b => b.PageBodyEn).HasMaxLength(HomeBanner.MaximumPageBodyLength);
        builder.Property(b => b.PageButtonLabelAz).HasMaxLength(HomeBanner.MaximumButtonLabelLength);
        builder.Property(b => b.PageButtonLabelRu).HasMaxLength(HomeBanner.MaximumButtonLabelLength);
        builder.Property(b => b.PageButtonLabelEn).HasMaxLength(HomeBanner.MaximumButtonLabelLength);
        builder.Property(b => b.PageButtonUrl).HasMaxLength(HomeBanner.MaximumLinkLength);
        builder.Property(b => b.Platform).HasDefaultValue(3);
        builder.Property(b => b.ShowText).HasDefaultValue(true);
        builder.Property(b => b.DarkOverlay).HasDefaultValue(true);

        builder.HasIndex(b => new { b.IsActive, b.DisplayOrder });
        builder.HasIndex(b => b.PageSlug).IsUnique().HasFilter("\"PageSlug\" IS NOT NULL");
    }
}
