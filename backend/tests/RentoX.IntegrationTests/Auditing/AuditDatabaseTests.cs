using Microsoft.EntityFrameworkCore;
using Npgsql;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Auditing;
using RentoX.Domain.Auditing;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings.Enums;
using RentoX.Infrastructure.Auditing;
using RentoX.Infrastructure.Persistence;

namespace RentoX.IntegrationTests.Auditing;

public sealed class AuditDatabaseTests(
    AuditPostgresFixture fixture)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset TestNow =
        new(2026, 10, 6, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AuditEntryCanBeSavedAndRead()
    {
        AuditLogEntry entry = CreateEntry(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TestNow);

        await SeedAsync(entry);

        await using RentoXDbContext db = fixture.CreateContext();

        AuditLogEntry stored =
            await db.Set<AuditLogEntry>()
                .AsNoTracking()
                .SingleAsync(item => item.Id == entry.Id);

        Assert.Equal(entry.OperationId, stored.OperationId);
        Assert.Equal(entry.ActorUserId, stored.ActorUserId);
        Assert.Equal(entry.TargetId, stored.TargetId);
        Assert.Equal(AuditAction.ListingStatusChanged, stored.Action);
        Assert.Equal((int?)ListingStatus.PendingReview, stored.PreviousValue);
        Assert.Equal((int?)ListingStatus.Active, stored.CurrentValue);
        Assert.Equal(TestNow, stored.OccurredAtUtc);
    }

    [Theory]
    [InlineData("UPDATE audit.entries SET \"CurrentValue\" = 4")]
    [InlineData("DELETE FROM audit.entries")]
    [InlineData("TRUNCATE TABLE audit.entries")]
    public async Task ExistingAuditEntriesCannotBeMutated(string statement)
    {
        AuditLogEntry entry = CreateEntry(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TestNow);

        await SeedAsync(entry);

        await using (RentoXDbContext db = fixture.CreateContext())
        {
            PostgresException error =
                await Assert.ThrowsAsync<PostgresException>(
                    () => db.Database.ExecuteSqlRawAsync(statement));

            Assert.Equal("55000", error.SqlState);
            Assert.Equal("Audit entries are append-only.", error.MessageText);
        }

        await using RentoXDbContext verification = fixture.CreateContext();

        AuditLogEntry stored =
            await verification.Set<AuditLogEntry>()
                .AsNoTracking()
                .SingleAsync(item => item.Id == entry.Id);

        Assert.Equal(entry.CurrentValue, stored.CurrentValue);
    }

    [Fact]
    public async Task DuplicateOperationActionAndTargetAreRejected()
    {
        Guid operationId = Guid.NewGuid();
        Guid actorId = Guid.NewGuid();
        Guid targetId = Guid.NewGuid();

        await SeedAsync(
            CreateEntry(operationId, actorId, targetId, TestNow));

        await using RentoXDbContext db = fixture.CreateContext();

        db.Set<AuditLogEntry>().Add(
            CreateEntry(operationId, actorId, targetId, TestNow));

        DbUpdateException error =
            await Assert.ThrowsAsync<DbUpdateException>(
                () => db.SaveChangesAsync());

        PostgresException postgres =
            Assert.IsType<PostgresException>(error.InnerException);

        Assert.Equal("23505", postgres.SqlState);
        Assert.Equal(
            "IX_entries_OperationId_Action_TargetId",
            postgres.ConstraintName);
    }

    [Fact]
    public async Task DatabaseRejectsUnchangedAuditPayload()
    {
        await using RentoXDbContext db = fixture.CreateContext();

        AuditLogEntry entry = CreateEntry(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TestNow);

        db.Set<AuditLogEntry>().Add(entry);

        // Deliberately bypass domain validation to test the DB constraint.
        db.Entry(entry)
            .Property(item => item.CurrentValue)
            .CurrentValue = entry.PreviousValue;

        DbUpdateException error =
            await Assert.ThrowsAsync<DbUpdateException>(
                () => db.SaveChangesAsync());

        PostgresException postgres =
            Assert.IsType<PostgresException>(error.InnerException);

        Assert.Equal("23514", postgres.SqlState);
        Assert.Equal("CK_audit_entries_Payload", postgres.ConstraintName);
    }

    [Fact]
    public async Task FailedAuditWriteCanRollBackAnEarlierBusinessSave()
    {
        Guid operationId = Guid.NewGuid();

        // A category is a transaction probe, not an audited category action.
        Category category = Category.Create(
            null,
            $"audit-transaction-{Guid.NewGuid():N}",
            null,
            0);

        await using (RentoXDbContext db = fixture.CreateContext())
        {
            await using var transaction =
                await db.Database.BeginTransactionAsync();

            db.Categories.Add(category);
            await db.SaveChangesAsync();

            // Prove that the first SaveChanges reached this transaction.
            Assert.True(
                await db.Categories
                    .AsNoTracking()
                    .AnyAsync(item => item.Id == category.Id));

            AuditLogEntry entry = CreateEntry(
                operationId, Guid.NewGuid(), Guid.NewGuid(), TestNow);

            db.Set<AuditLogEntry>().Add(entry);

            db.Entry(entry)
                .Property(item => item.CurrentValue)
                .CurrentValue = entry.PreviousValue;

            DbUpdateException error =
                await Assert.ThrowsAsync<DbUpdateException>(
                    () => db.SaveChangesAsync());

            PostgresException postgres =
                Assert.IsType<PostgresException>(error.InnerException);

            Assert.Equal("23514", postgres.SqlState);

            await transaction.RollbackAsync();
        }

        await using RentoXDbContext verification = fixture.CreateContext();

        Assert.False(
            await verification.Categories
                .AsNoTracking()
                .AnyAsync(item => item.Id == category.Id));

        Assert.False(
            await verification.Set<AuditLogEntry>()
                .AsNoTracking()
                .AnyAsync(item => item.OperationId == operationId));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task SearchAppliesActorTargetAndActionFilters(int filter)
    {
        Guid operationId = Guid.NewGuid();
        Guid actorId = Guid.NewGuid();
        Guid targetId = Guid.NewGuid();

        AuditLogEntry expected = CreateEntry(
            operationId, actorId, targetId, TestNow.AddHours(-1));

        AuditLogEntry other = AuditLogEntry.ForSupportReply(
            operationId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            TestNow.AddHours(-1));

        await SeedAsync(expected, other);

        AuditLogSearchQuery query = CreateQuery(operationId);

        query = filter switch
        {
            1 => query with { ActorUserId = actorId },
            2 => query with { TargetId = targetId },
            3 => query with { Action = (int)AuditAction.ListingStatusChanged },
            _ => throw new ArgumentOutOfRangeException(nameof(filter))
        };

        AuditLogPageResult result = await SearchAsync(query);

        Assert.Equal(expected.Id, Assert.Single(result.Items).Id);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task SearchDoesNotReturnAnotherOperation()
    {
        Guid operationId = Guid.NewGuid();

        AuditLogEntry expected = CreateEntry(
            operationId, Guid.NewGuid(), Guid.NewGuid(), TestNow.AddHours(-1));

        AuditLogEntry other = CreateEntry(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TestNow.AddHours(-1));

        await SeedAsync(expected, other);

        AuditLogPageResult result =
            await SearchAsync(CreateQuery(operationId));

        Assert.Equal(expected.Id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task DateRangeIncludesStartAndExcludesEnd()
    {
        Guid operationId = Guid.NewGuid();
        DateTimeOffset fromUtc = TestNow.AddDays(-2);
        DateTimeOffset toUtc = TestNow.AddDays(-1);

        AuditLogEntry before = CreateEntry(
            operationId, Guid.NewGuid(), Guid.NewGuid(), fromUtc.AddSeconds(-1));

        AuditLogEntry atStart = CreateEntry(
            operationId, Guid.NewGuid(), Guid.NewGuid(), fromUtc);

        AuditLogEntry inside = CreateEntry(
            operationId, Guid.NewGuid(), Guid.NewGuid(), toUtc.AddSeconds(-1));

        AuditLogEntry atEnd = CreateEntry(
            operationId, Guid.NewGuid(), Guid.NewGuid(), toUtc);

        await SeedAsync(before, atStart, inside, atEnd);

        AuditLogPageResult result = await SearchAsync(
            CreateQuery(operationId) with
            {
                FromUtc = fromUtc.ToOffset(TimeSpan.FromHours(4)),
                ToUtc = toUtc.ToOffset(TimeSpan.FromHours(4))
            });

        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, item => item.Id == atStart.Id);
        Assert.Contains(result.Items, item => item.Id == inside.Id);
        Assert.DoesNotContain(result.Items, item => item.Id == before.Id);
        Assert.DoesNotContain(result.Items, item => item.Id == atEnd.Id);
        Assert.Equal(TimeSpan.Zero, result.FromUtc.Offset);
        Assert.Equal(TimeSpan.Zero, result.ToUtc.Offset);
    }

    [Fact]
    public async Task DefaultRangeUsesTheLastThirtyDays()
    {
        Guid operationId = Guid.NewGuid();

        AuditLogEntry oldEntry = CreateEntry(
            operationId, Guid.NewGuid(), Guid.NewGuid(), TestNow.AddDays(-31));

        AuditLogEntry atStart = CreateEntry(
            operationId, Guid.NewGuid(), Guid.NewGuid(), TestNow.AddDays(-30));

        AuditLogEntry recent = CreateEntry(
            operationId, Guid.NewGuid(), Guid.NewGuid(), TestNow.AddHours(-1));

        await SeedAsync(oldEntry, atStart, recent);

        AuditLogPageResult result =
            await SearchAsync(CreateQuery(operationId));

        Assert.Equal(TestNow.AddDays(-30), result.FromUtc);
        Assert.Equal(TestNow, result.ToUtc);
        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, item => item.Id == atStart.Id);
        Assert.Contains(result.Items, item => item.Id == recent.Id);
        Assert.DoesNotContain(result.Items, item => item.Id == oldEntry.Id);
    }

    [Fact]
    public async Task PaginationHandlesIdenticalTimestamps()
    {
        Guid operationId = Guid.NewGuid();
        DateTimeOffset time = TestNow.AddHours(-1);

        AuditLogEntry first = CreateEntry(
            operationId, Guid.NewGuid(), Guid.NewGuid(), time);

        AuditLogEntry second = CreateEntry(
            operationId, Guid.NewGuid(), Guid.NewGuid(), time);

        AuditLogEntry third = CreateEntry(
            operationId, Guid.NewGuid(), Guid.NewGuid(), time);

        await SeedAsync(first, second, third);

        AuditLogSearchQuery query =
            CreateQuery(operationId) with { PageSize = 1 };

        var found = new HashSet<Guid>();

        for (int page = 1; page <= 3; page++)
        {
            AuditLogPageResult result =
                await SearchAsync(query with { Page = page });

            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.TotalPages);

            Guid id = Assert.Single(result.Items).Id;

            Assert.True(found.Add(id));

            AuditLogPageResult repeated =
                await SearchAsync(query with { Page = page });

            Assert.Equal(id, Assert.Single(repeated.Items).Id);
        }

        Assert.Contains(first.Id, found);
        Assert.Contains(second.Id, found);
        Assert.Contains(third.Id, found);

        AuditLogPageResult emptyPage =
            await SearchAsync(query with { Page = 4 });

        Assert.Empty(emptyPage.Items);
        Assert.Equal(3, emptyPage.TotalCount);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    public async Task InvalidSearchParametersAreRejected(int scenario)
    {
        AuditLogSearchQuery query = CreateQuery(Guid.NewGuid());

        query = scenario switch
        {
            1 => query with { Page = 0 },
            2 => query with { Page = 1001 },
            3 => query with { PageSize = 0 },
            4 => query with { PageSize = 101 },
            5 => query with { ActorUserId = Guid.Empty },
            6 => query with { TargetId = Guid.Empty },
            7 => query with { OperationId = Guid.Empty },
            8 => query with { Action = 999 },
            9 => query with { FromUtc = TestNow.AddDays(-1) },
            10 => query with
            {
                FromUtc = TestNow,
                ToUtc = TestNow
            },
            11 => query with
            {
                FromUtc = TestNow.AddDays(-91),
                ToUtc = TestNow
            },
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };

        await Assert.ThrowsAsync<DomainException>(
            () => SearchAsync(query));
    }

    private async Task SeedAsync(params AuditLogEntry[] entries)
    {
        await using RentoXDbContext db = fixture.CreateContext();

        db.Set<AuditLogEntry>().AddRange(entries);
        await db.SaveChangesAsync();
    }

    private async Task<AuditLogPageResult> SearchAsync(
        AuditLogSearchQuery query)
    {
        await using RentoXDbContext db = fixture.CreateContext();

        var service = new AuditLogQueryService(
            db,
            new FixedClock(TestNow));

        return await service.SearchAsync(query);
    }

    private static AuditLogSearchQuery CreateQuery(Guid operationId)
    {
        return new AuditLogSearchQuery(
            null,
            null,
            operationId,
            null,
            null,
            null,
            1,
            20);
    }

    private static AuditLogEntry CreateEntry(
        Guid operationId,
        Guid actorId,
        Guid targetId,
        DateTimeOffset time)
    {
        return AuditLogEntry.ForListingStatusChange(
            operationId,
            actorId,
            targetId,
            ListingStatus.PendingReview,
            ListingStatus.Active,
            time);
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}