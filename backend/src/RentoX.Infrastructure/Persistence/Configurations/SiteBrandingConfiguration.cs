using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Content;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class SiteBrandingConfiguration : IEntityTypeConfiguration<SiteBranding>
{
    public void Configure(EntityTypeBuilder<SiteBranding> builder)
    {
        builder.ToTable("site_branding", "content");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.SiteName).HasMaxLength(SiteBranding.MaximumNameLength).IsRequired();
        builder.Property(b => b.FooterText).HasMaxLength(SiteBranding.MaximumFooterTextLength);
        builder.Property(b => b.HeaderLogoKey).HasMaxLength(300);
        builder.Property(b => b.HeaderLogoDarkKey).HasMaxLength(300);
        builder.Property(b => b.FooterLogoKey).HasMaxLength(300);
        builder.Property(b => b.FooterLogoDarkKey).HasMaxLength(300);
        builder.Property(b => b.IconKey).HasMaxLength(300);
        builder.Property(b => b.ShareImageKey).HasMaxLength(300);
        builder.Property(b => b.Description).HasMaxLength(SiteBranding.MaximumDescriptionLength);
        builder.Property(b => b.AnnouncementText).HasMaxLength(SiteBranding.MaximumAnnouncementLength);
        builder.Property(b => b.AnnouncementLink).HasMaxLength(SiteBranding.MaximumAnnouncementLinkLength);
        builder.Property(b => b.HeaderLogoHeight).HasDefaultValue(SiteBranding.DefaultLogoHeight);
        builder.Property(b => b.FooterLogoHeight).HasDefaultValue(SiteBranding.DefaultLogoHeight);
    }
}
