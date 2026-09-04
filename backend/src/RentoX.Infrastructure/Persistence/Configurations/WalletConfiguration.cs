using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Wallets;
using RentoX.Infrastructure.Identity;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class WalletConfiguration
    : IEntityTypeConfiguration<Wallet>
{
    public void Configure(
        EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable(
            "wallets",
            "payments");

        builder.HasKey(wallet => wallet.Id);

        builder.Property(wallet => wallet.UserId)
            .IsRequired();

        builder.Property(wallet => wallet.Balance)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(wallet => wallet.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(wallet => wallet.Version)
            .IsRowVersion();

        builder.HasIndex(wallet => wallet.UserId)
            .IsUnique();

        builder.HasOne<AppUser>()
            .WithOne()
            .HasForeignKey<Wallet>(
                wallet => wallet.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(wallet =>
                wallet.Transactions)
            .WithOne()
            .HasForeignKey(transaction =>
                transaction.WalletId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(wallet =>
                wallet.Transactions)
            .UsePropertyAccessMode(
                PropertyAccessMode.Field);
    }
}
