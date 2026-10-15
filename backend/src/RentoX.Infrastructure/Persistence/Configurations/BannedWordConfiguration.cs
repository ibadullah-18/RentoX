using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Moderation;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class BannedWordConfiguration : IEntityTypeConfiguration<BannedWord>
{
    public void Configure(EntityTypeBuilder<BannedWord> builder)
    {
        builder.ToTable("banned_words", "moderation");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Word).HasMaxLength(BannedWord.MaximumLength).IsRequired();
        builder.Property(w => w.CreatedByUserId).IsRequired();
        builder.Property(w => w.CreatedAtUtc).IsRequired();

        builder.HasIndex(w => w.Word).IsUnique();
    }
}
