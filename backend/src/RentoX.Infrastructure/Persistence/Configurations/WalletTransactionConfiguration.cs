using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Wallets;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class WalletTransactionConfiguration
    : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(
        EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.ToTable(
            "wallet_transactions",
            "payments");

        builder.HasKey(transaction =>
            transaction.Id);

        builder.Property(transaction =>
                transaction.WalletId)
            .IsRequired();

        builder.Property(transaction =>
                transaction.Direction)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(transaction =>
                transaction.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(transaction =>
                transaction.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(transaction =>
                transaction.BalanceBefore)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(transaction =>
                transaction.BalanceAfter)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(transaction =>
                transaction.Reason)
            .HasMaxLength(
                WalletTransaction
                    .MaximumReasonLength)
            .IsRequired();

        builder.Property(transaction =>
                transaction.IdempotencyKey)
            .HasMaxLength(
                WalletTransaction
                    .MaximumIdempotencyKeyLength)
            .IsRequired();

        builder.Property(transaction =>
                transaction.OccurredAtUtc)
            .IsRequired();

        builder.HasIndex(transaction =>
                transaction.IdempotencyKey)
            .IsUnique();

        builder.HasIndex(transaction => new
        {
            transaction.WalletId,
            transaction.OccurredAtUtc
        });

        builder.HasIndex(transaction =>
            transaction.RelatedEntityId);
    }
}
