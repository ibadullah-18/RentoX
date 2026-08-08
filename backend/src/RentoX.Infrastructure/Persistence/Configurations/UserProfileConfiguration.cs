using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Users;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class UserProfileConfiguration
    : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(
        EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles", "users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FullName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Bio)
            .HasMaxLength(500);

        builder.Property(x => x.ProfileImageKey)
            .HasMaxLength(500);

        builder.Property(x => x.PreferredLanguage)
            .HasConversion<int>();

        builder.Property(x => x.Status)
            .HasConversion<int>();

        builder.HasIndex(x => x.Status);

        builder.HasIndex(x => x.CreatedAtUtc);

        builder.HasQueryFilter(x => x.DeletedAtUtc == null);

        builder.Ignore(x => x.DomainEvents);
    }
}