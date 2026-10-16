using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Hr;
using RentoX.Infrastructure.Identity;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class StaffProfileConfiguration : IEntityTypeConfiguration<StaffProfile>
{
    public void Configure(EntityTypeBuilder<StaffProfile> builder)
    {
        builder.ToTable("staff_profiles", "hr", t =>
        {
            t.HasCheckConstraint("CK_staff_profiles_Salary", "\"MonthlySalaryInQepik\" BETWEEN 0 AND 10000000");
            t.HasCheckConstraint("CK_staff_profiles_Hours", "\"WorkStartMinute\" BETWEEN 0 AND 1439 AND \"WorkEndMinute\" BETWEEN 1 AND 1440 AND \"WorkEndMinute\" > \"WorkStartMinute\"");
            t.HasCheckConstraint("CK_staff_profiles_Days", "\"WorkDaysMask\" BETWEEN 0 AND 127 AND \"MonthlyDayOffLimit\" BETWEEN 0 AND 31");
        });
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Note).HasMaxLength(500);
        builder.HasIndex(p => p.UserId).IsUnique();
        builder.HasOne<AppUser>().WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("attendance_records", "hr");
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => new { r.UserId, r.Day }).IsUnique();
        builder.HasIndex(r => r.Day);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("leave_requests", "hr", t =>
            t.HasCheckConstraint("CK_leave_requests_Status", "\"Status\" BETWEEN 1 AND 4"));
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Status).HasConversion<int>().IsRequired();
        builder.Property(r => r.Reason).HasMaxLength(LeaveRequest.MaximumReasonLength).IsRequired();
        builder.Property(r => r.DecisionNote).HasMaxLength(LeaveRequest.MaximumReasonLength);
        builder.HasIndex(r => new { r.UserId, r.FromDay });
        builder.HasIndex(r => r.Status);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PayrollEntryConfiguration : IEntityTypeConfiguration<PayrollEntry>
{
    public void Configure(EntityTypeBuilder<PayrollEntry> builder)
    {
        builder.ToTable("payroll_entries", "hr", t =>
        {
            t.HasCheckConstraint("CK_payroll_entries_Kind", "\"Kind\" BETWEEN 1 AND 4");
            t.HasCheckConstraint("CK_payroll_entries_Amount", "\"AmountInQepik\" > 0");
        });
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Kind).HasConversion<int>().IsRequired();
        builder.Property(e => e.Note).HasMaxLength(PayrollEntry.MaximumNoteLength);
        builder.Ignore(e => e.IsVoid);
        builder.HasIndex(e => new { e.UserId, e.Month });
        builder.HasOne<AppUser>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class StaffRequestConfiguration : IEntityTypeConfiguration<StaffRequest>
{
    public void Configure(EntityTypeBuilder<StaffRequest> builder)
    {
        builder.ToTable("staff_requests", "hr", t =>
            t.HasCheckConstraint("CK_staff_requests_Status", "\"Status\" BETWEEN 1 AND 4"));
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Status).HasConversion<int>().IsRequired();
        builder.Property(r => r.Subject).HasMaxLength(StaffRequest.MaximumSubjectLength).IsRequired();
        builder.Property(r => r.Body).HasMaxLength(StaffRequest.MaximumBodyLength).IsRequired();
        builder.Property(r => r.Answer).HasMaxLength(StaffRequest.MaximumBodyLength);
        builder.HasIndex(r => new { r.Status, r.CreatedAtUtc });
        builder.HasIndex(r => r.FromUserId);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(r => r.FromUserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AttendanceAlertConfiguration : IEntityTypeConfiguration<AttendanceAlert>
{
    public void Configure(EntityTypeBuilder<AttendanceAlert> builder)
    {
        builder.ToTable("attendance_alerts", "hr");
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => new { a.UserId, a.Day }).IsUnique();
        builder.HasOne<AppUser>().WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class StaffSalaryChangeConfiguration : IEntityTypeConfiguration<StaffSalaryChange>
{
    public void Configure(EntityTypeBuilder<StaffSalaryChange> builder)
    {
        builder.ToTable("staff_salary_changes", "hr", t =>
        {
            t.HasCheckConstraint("CK_staff_salary_changes_Amount", "\"AmountInQepik\" BETWEEN 0 AND 10000000");
        });
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => new { c.UserId, c.EffectiveFrom });
        builder.HasOne<AppUser>().WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
