using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Messaging;
using RentoX.Infrastructure.Identity;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class UserBlockConfiguration : IEntityTypeConfiguration<UserBlock>
{
    public void Configure(EntityTypeBuilder<UserBlock> builder)
    {
        builder.ToTable("user_blocks", "messaging", table =>
            table.HasCheckConstraint("CK_user_blocks_DifferentUsers",
                "\"BlockerId\" <> \"BlockedUserId\""));
        builder.HasKey(block => block.Id);
        builder.Property(block => block.CreatedAtUtc).IsRequired();
        builder.HasIndex(block => new { block.BlockerId, block.BlockedUserId }).IsUnique();
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(block => block.BlockerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(block => block.BlockedUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
