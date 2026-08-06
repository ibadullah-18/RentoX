using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Authentication;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class OtpChallengeConfiguration
    : IEntityTypeConfiguration<OtpChallenge>
{
    public void Configure(
        EntityTypeBuilder<OtpChallenge> builder)
    {
        builder.ToTable(
            "otp_challenges",
            "authentication");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PhoneNumber)
            .HasMaxLength(13)
            .IsRequired();

        builder.Property(x => x.CodeHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Purpose)
            .HasConversion<int>();

        builder.HasIndex(x => new
        {
            x.PhoneNumber,
            x.Purpose,
            x.CreatedAtUtc
        });

        builder.Ignore(x => x.DomainEvents);
    }
}