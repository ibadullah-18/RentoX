using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Content;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class ContentPageConfiguration : IEntityTypeConfiguration<ContentPage>
{
    public void Configure(EntityTypeBuilder<ContentPage> builder)
    {
        builder.ToTable("pages", "content");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Slug).HasMaxLength(40).IsRequired();
        builder.Property(p => p.TitleAz).HasMaxLength(ContentPage.MaximumTitleLength).IsRequired();
        builder.Property(p => p.TitleRu).HasMaxLength(ContentPage.MaximumTitleLength).IsRequired();
        builder.Property(p => p.TitleEn).HasMaxLength(ContentPage.MaximumTitleLength).IsRequired();
        builder.Property(p => p.BodyAz).IsRequired();
        builder.Property(p => p.BodyRu).IsRequired();
        builder.Property(p => p.BodyEn).IsRequired();
        builder.Property(p => p.UpdatedAtUtc).IsRequired();

        builder.HasIndex(p => p.Slug).IsUnique();
    }
}

public sealed class SiteInfoConfiguration : IEntityTypeConfiguration<SiteInfo>
{
    public void Configure(EntityTypeBuilder<SiteInfo> builder)
    {
        builder.ToTable("site_info", "content");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.OperatorName).HasMaxLength(150);
        builder.Property(s => s.Voen).HasMaxLength(30);
        builder.Property(s => s.Address).HasMaxLength(300);
        builder.Property(s => s.Email).HasMaxLength(150);
        builder.Property(s => s.Phone).HasMaxLength(25);
        builder.Property(s => s.Hotline).HasMaxLength(25);
        builder.Property(s => s.WhatsApp).HasMaxLength(30);
        builder.Property(s => s.Instagram).HasMaxLength(300);
        builder.Property(s => s.TikTok).HasMaxLength(300);
        builder.Property(s => s.Facebook).HasMaxLength(300);
        builder.Property(s => s.UpdatedAtUtc).IsRequired();
    }
}
