using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Stores;
using RentoX.Infrastructure.Identity;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class StoreFollowerConfiguration
    : IEntityTypeConfiguration<StoreFollower>
{
    public void Configure(
        EntityTypeBuilder<StoreFollower> builder)
    {
        builder.ToTable(
            "store_followers",
            "engagement");

        builder.HasKey(follower => follower.Id);

        builder.Property(follower =>
                follower.UserId)
            .IsRequired();

        builder.Property(follower =>
                follower.StoreId)
            .IsRequired();

        builder.Property(follower =>
                follower.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(follower => new
        {
            follower.UserId,
            follower.StoreId
        })
            .IsUnique();

        builder.HasIndex(follower => new
        {
            follower.StoreId,
            follower.CreatedAtUtc
        });

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(follower =>
                follower.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<StoreProfile>()
            .WithMany()
            .HasForeignKey(follower =>
                follower.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}