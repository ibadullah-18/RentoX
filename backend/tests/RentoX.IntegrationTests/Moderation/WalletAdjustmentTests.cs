using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Admin;
using RentoX.Domain.Auditing;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Domain.Wallets.Enums;
using RentoX.Infrastructure.Admin;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Persistence;
using RentoX.Infrastructure.Wallets;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Moderation;

/// <summary>A bonus, a refund or a correction made by hand: limits, the audit trail and the double click.</summary>
public sealed class WalletAdjustmentTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

    private const int Bonus = (int)WalletTransactionType.Bonus;

    private const int Refund = (int)WalletTransactionType.Refund;

    private const int Correction = (int)WalletTransactionType.AdminAdjustment;

    [Fact]
    public async Task ACreditAndADebitMoveTheBalanceAndEachWritesItsAudit()
    {
        Guid admin = await AddUserAsync();
        Guid person = await AddUserAsync();
        Guid bonusRequest = Guid.NewGuid();
        Guid takeRequest = Guid.NewGuid();

        WalletAdjustmentResult added = await Service(admin).AdjustAsync(person, bonusRequest, true, Bonus, 25m, "Yeni il bonusu");
        Assert.Equal(25m, added.BalanceAfter);
        Assert.False(added.AlreadyDone);

        WalletAdjustmentResult taken = await Service(admin).AdjustAsync(person, takeRequest, false, Correction, 10.50m, "Səhv əlavə olunan məbləğ");
        Assert.Equal(14.50m, taken.BalanceAfter);

        await using RentoXDbContext db = database.CreateContext();
        AuditLogEntry credit = await db.Set<AuditLogEntry>().SingleAsync(a => a.OperationId == bonusRequest);
        Assert.Equal(AuditAction.WalletAdjusted, credit.Action);
        Assert.Equal(admin, credit.ActorUserId);
        Assert.Equal(person, credit.TargetId);
        Assert.Equal(2500, credit.CurrentValue);
        Assert.NotNull(credit.RelatedEntityId);

        AuditLogEntry debit = await db.Set<AuditLogEntry>().SingleAsync(a => a.OperationId == takeRequest);
        Assert.Equal(-1050, debit.CurrentValue);

        // The person sees the reason in their history.
        var moves = await db.Set<RentoX.Domain.Wallets.WalletTransaction>()
            .Where(t => t.Id == credit.RelatedEntityId)
            .ToListAsync();
        Assert.Equal("Yeni il bonusu", moves.Single().Reason);
    }

    [Fact]
    public async Task TheSameRequestSentTwiceChangesTheWalletOnce()
    {
        Guid admin = await AddUserAsync();
        Guid person = await AddUserAsync();
        Guid request = Guid.NewGuid();

        await Service(admin).AdjustAsync(person, request, true, Refund, 5m, "Geri qaytarma #1");
        WalletAdjustmentResult second = await Service(admin).AdjustAsync(person, request, true, Refund, 5m, "Geri qaytarma #1");

        Assert.True(second.AlreadyDone);
        Assert.Equal(5m, second.BalanceAfter);
        await using RentoXDbContext db = database.CreateContext();
        Assert.Equal(1, await db.Set<AuditLogEntry>().CountAsync(a => a.OperationId == request));
    }

    [Theory]
    [InlineData(0, true, Bonus)]
    [InlineData(-5, true, Bonus)]
    [InlineData(1000.01, true, Bonus)]
    [InlineData(1.005, true, Bonus)]
    [InlineData(5, false, Bonus)]      // a debit is only a correction
    [InlineData(5, true, 2)]           // a listing fee cannot be made up by hand
    [InlineData(5, true, 99)]
    public async Task OutOfRangeOrWrongKindIsRefusedAndNothingChanges(double amount, bool credit, int type)
    {
        Guid admin = await AddUserAsync();
        Guid person = await AddUserAsync();
        Guid request = Guid.NewGuid();

        await Assert.ThrowsAsync<DomainException>(
            () => Service(admin).AdjustAsync(person, request, credit, type, (decimal)amount, "Yoxlama səbəbi"));

        await using RentoXDbContext db = database.CreateContext();
        Assert.False(await db.Set<AuditLogEntry>().AnyAsync(a => a.OperationId == request));
    }

    [Fact]
    public async Task AReasonIsRequiredYouCannotChangeYourOwnWalletAndTheBalanceCannotGoNegative()
    {
        Guid admin = await AddUserAsync();
        Guid person = await AddUserAsync();

        await Assert.ThrowsAsync<DomainException>(
            () => Service(admin).AdjustAsync(person, Guid.NewGuid(), true, Bonus, 5m, "  "));
        await Assert.ThrowsAsync<DomainException>(
            () => Service(admin).AdjustAsync(person, Guid.NewGuid(), true, Bonus, 5m, "Qısa"));
        await Assert.ThrowsAsync<DomainException>(
            () => Service(admin).AdjustAsync(admin, Guid.NewGuid(), true, Bonus, 5m, "Özümə bonus"));
        await Assert.ThrowsAsync<DomainException>(
            () => Service(admin).AdjustAsync(Guid.NewGuid(), Guid.NewGuid(), true, Bonus, 5m, "Olmayan istifadəçi"));

        // Taking more than there is.
        Guid request = Guid.NewGuid();
        await Assert.ThrowsAsync<DomainException>(
            () => Service(admin).AdjustAsync(person, request, false, Correction, 50m, "Boş kisədən götürmə"));
        await using RentoXDbContext db = database.CreateContext();
        Assert.False(await db.Set<AuditLogEntry>().AnyAsync(a => a.OperationId == request));
    }

    private WalletAdjustmentService Service(Guid actor)
    {
        RentoXDbContext db = database.CreateContext();
        return new WalletAdjustmentService(db, new WalletService(db, new TestClock()), new TestClock(), new TestActor(actor));
    }

    private async Task<Guid> AddUserAsync()
    {
        await using RentoXDbContext db = database.CreateContext();
        Guid id = Guid.NewGuid();
        string userName = $"wadj-{id:N}";
        db.Users.Add(new AppUser
        {
            Id = id,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            PhoneNumber = "+99450" + Random.Shared.Next(1000000, 9999999),
            RegisteredAtUtc = Now,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        });
        db.Set<UserProfile>().Add(UserProfile.Create(id, "Test " + id.ToString("N")[..6], PreferredLanguage.Azerbaijani));
        await db.SaveChangesAsync();
        return id;
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class TestActor(Guid userId) : ICurrentUserContext
    {
        public Guid? UserId => userId;

        public bool IsAuthenticated => true;
    }
}
