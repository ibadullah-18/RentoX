using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Finance;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class FinanceExpenseConfiguration : IEntityTypeConfiguration<FinanceExpense>
{
    public void Configure(EntityTypeBuilder<FinanceExpense> builder)
    {
        builder.ToTable("expenses", "finance", t =>
        {
            t.HasCheckConstraint("CK_expenses_Amount", "\"Amount\" > 0");
        });
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Title).HasMaxLength(FinanceExpense.MaximumTitleLength).IsRequired();
        builder.Property(e => e.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(e => e.Note).HasMaxLength(FinanceExpense.MaximumNoteLength);
        builder.Property(e => e.VoidReason).HasMaxLength(FinanceExpense.MaximumNoteLength);
        builder.HasIndex(e => e.Day);
    }
}

public sealed class FinanceProfileConfiguration : IEntityTypeConfiguration<FinanceProfile>
{
    public void Configure(EntityTypeBuilder<FinanceProfile> builder)
    {
        builder.ToTable("profile", "finance");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.FullName).HasMaxLength(FinanceProfile.MaximumLength).IsRequired();
        builder.Property(p => p.Voen).HasMaxLength(FinanceProfile.MaximumLength);
        builder.Property(p => p.Phone).HasMaxLength(FinanceProfile.MaximumLength);
        builder.Property(p => p.Email).HasMaxLength(FinanceProfile.MaximumLength);
        builder.Property(p => p.Address).HasMaxLength(FinanceProfile.MaximumLength);
    }
}
