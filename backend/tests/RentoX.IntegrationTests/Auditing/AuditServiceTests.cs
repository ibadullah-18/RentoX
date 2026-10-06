using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Common;
using RentoX.Application.Listings.Billing;
using RentoX.Application.Notifications;
using RentoX.Application.Wallets;
using RentoX.Domain.Auditing;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Stores;
using RentoX.Domain.Stores.Enums;
using RentoX.Domain.Support;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Listings;
using RentoX.Infrastructure.Persistence;
using RentoX.Infrastructure.Persistence.Interceptors;
using RentoX.Infrastructure.Stores;
using RentoX.Infrastructure.Support;

namespace RentoX.IntegrationTests.Auditing;

public sealed class AuditServiceTests(
    AuditPostgresFixture fixture)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset TestNow =
        new(2026, 10, 6, 8, 0, 0, TimeSpan.Zero);

    private static readonly TestClock Clock = new();

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task ListingModerationPreservesAuditAtomicity(
        bool approve,
        bool rejectAudit)
    {
        TestUsers users = await SeedUsersAsync();

        Category category = Category.Create(
            null, $"audit-{Guid.NewGuid():N}", null, 0);

        Listing listing = Listing.Create(
            users.OwnerId,
            category.Id,
            "Audit test listing",
            "Listing created only for an isolated integration test.",
            100m,
            "AZN",
            (RentalPeriodUnit)2);

        listing.MarkAsCreated(TestNow.AddDays(-1), users.OwnerId);
        listing.AddImage($"tests/{listing.Id:N}/listing.jpg", "image/jpeg", 1024);
        listing.SubmitForReview();

        await using (RentoXDbContext seed = fixture.CreateContext())
        {
            seed.Categories.Add(category);
            seed.Listings.Add(listing);
            await seed.SaveChangesAsync();
        }

        var actor = new TestActor(users.AdminId);
        var probe = new AuditFailureInterceptor(rejectAudit);
        var notifications = new NotificationSpy(fixture, listing.Id);

        await using (RentoXDbContext db = CreateServiceContext(actor, probe))
        {
            var service = new ListingModerationService(
                dbContext: db,
                clock: Clock,
                pricingService: new FreePricing(),
                walletService: new UnusedWallet(),
                notificationService: notifications,
                currentUserContext: actor);

            Func<Task> operation = approve
                ? () => service.ApproveAsync(listing.Id)
                : () => service.RejectAsync(listing.Id, "Test rejection");

            await ExecuteAsync(operation, rejectAudit, probe);
        }

        await using RentoXDbContext verification = fixture.CreateContext();

        Listing stored = await verification.Listings
            .AsNoTracking()
            .SingleAsync(item => item.Id == listing.Id);

        AuditLogEntry[] audits =
            await LoadAuditAsync(verification, listing.Id);

        int cycleCount = await verification.ListingBillingCycles
            .CountAsync(item => item.ListingId == listing.Id);

        if (rejectAudit)
        {
            Assert.Equal(ListingStatus.PendingReview, stored.Status);
            Assert.Null(stored.RejectionReason);
            Assert.Null(stored.PublishedAtUtc);
            Assert.Null(stored.ExpiresAtUtc);
            Assert.Empty(audits);
            Assert.Equal(0, cycleCount);
            Assert.Equal(0, notifications.CreateCalls);
        }
        else
        {
            ListingStatus expectedStatus = approve
                ? ListingStatus.Active
                : ListingStatus.Rejected;

            Assert.Equal(expectedStatus, stored.Status);

            AssertAudit(
                Assert.Single(audits),
                users.AdminId,
                listing.Id,
                AuditAction.ListingStatusChanged,
                (int)ListingStatus.PendingReview,
                (int)expectedStatus);

            Assert.Equal(approve ? 1 : 0, cycleCount);
            Assert.Equal(1, notifications.CreateCalls);

            if (approve)
            {
                Assert.Equal(TestNow, stored.PublishedAtUtc);
                Assert.Equal(TestNow.AddDays(30), stored.ExpiresAtUtc);
            }
            else
            {
                Assert.Equal("Test rejection", stored.RejectionReason);
            }
        }
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task StoreModerationPreservesAuditAtomicity(
        bool approve,
        bool rejectAudit)
    {
        TestUsers users = await SeedUsersAsync();

        StoreProfile store = StoreProfile.Create(
            users.OwnerId,
            "Audit test store",
            $"audit-{Guid.NewGuid():N}",
            "Store created only for an isolated integration test.",
            "+994500000000",
            null, null, null, null, null, null);

        store.MarkAsCreated(TestNow.AddDays(-1), users.OwnerId);
        store.SetLogo("tests/store-logo.png");
        store.SubmitForReview();

        await using (RentoXDbContext seed = fixture.CreateContext())
        {
            seed.StoreProfiles.Add(store);
            await seed.SaveChangesAsync();
        }

        var actor = new TestActor(users.AdminId);
        var probe = new AuditFailureInterceptor(rejectAudit);
        var notifications = new NotificationSpy(fixture, store.Id);

        await using (RentoXDbContext db = CreateServiceContext(actor, probe))
        {
            var service = new StoreModerationService(
                dbContext: db,
                notificationService: notifications,
                currentUserContext: actor,
                clock: Clock);

            Func<Task> operation = approve
                ? () => service.ApproveAsync(store.Id)
                : () => service.RejectAsync(store.Id, "Test rejection");

            await ExecuteAsync(operation, rejectAudit, probe);
        }

        await using RentoXDbContext verification = fixture.CreateContext();

        StoreProfile stored = await verification.StoreProfiles
            .AsNoTracking()
            .SingleAsync(item => item.Id == store.Id);

        AuditLogEntry[] audits =
            await LoadAuditAsync(verification, store.Id);

        if (rejectAudit)
        {
            Assert.Equal(StoreStatus.PendingReview, stored.Status);
            Assert.Null(stored.RejectionReason);
            Assert.Empty(audits);
            Assert.Equal(0, notifications.CreateCalls);
        }
        else
        {
            StoreStatus expectedStatus = approve
                ? StoreStatus.Active
                : StoreStatus.Rejected;

            Assert.Equal(expectedStatus, stored.Status);

            AssertAudit(
                Assert.Single(audits),
                users.AdminId,
                store.Id,
                AuditAction.StoreStatusChanged,
                (int)StoreStatus.PendingReview,
                (int)expectedStatus);

            Assert.Equal(1, notifications.CreateCalls);

            if (!approve)
            {
                Assert.Equal("Test rejection", stored.RejectionReason);
            }
        }
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    public async Task SupportChangesPreserveAuditAtomicity(
        int operationKind,
        bool rejectAudit)
    {
        TestUsers users = await SeedUsersAsync();

        SupportTicket ticket = SupportTicket.Create(
            users.OwnerId,
            SupportTicketCategory.General,
            "Audit integration test",
            "Initial support message.",
            TestNow.AddDays(-1));

        await using (RentoXDbContext seed = fixture.CreateContext())
        {
            seed.Set<SupportTicket>().Add(ticket);
            await seed.SaveChangesAsync();
        }

        var actor = new TestActor(users.AdminId);
        var probe = new AuditFailureInterceptor(rejectAudit);
        var notifications = new NotificationSpy(fixture, ticket.Id);

        await using (RentoXDbContext db = CreateServiceContext(actor, probe))
        {
            var service = new SupportTicketService(
                dbContext: db,
                clock: Clock,
                notificationService: notifications,
                currentUserContext: actor);

            Func<Task> operation = operationKind switch
            {
                1 => () => service.AddAdminMessageAsync(
                    users.AdminId, ticket.Id, "Admin test reply."),
                2 => () => service.ChangeStatusAsync(
                    users.AdminId, ticket.Id, (int)SupportTicketStatus.Resolved),
                3 => () => service.ChangePriorityAsync(
                    users.AdminId, ticket.Id, (int)SupportTicketPriority.High),
                _ => throw new ArgumentOutOfRangeException(nameof(operationKind))
            };

            await ExecuteAsync(operation, rejectAudit, probe);
        }

        await using RentoXDbContext verification = fixture.CreateContext();

        SupportTicket stored = await verification.Set<SupportTicket>()
            .AsNoTracking()
            .Include(item => item.Messages)
            .SingleAsync(item => item.Id == ticket.Id);

        AuditLogEntry[] audits =
            await LoadAuditAsync(verification, ticket.Id);

        if (rejectAudit)
        {
            Assert.Equal(SupportTicketStatus.Open, stored.Status);
            Assert.Equal(SupportTicketPriority.Normal, stored.Priority);
            Assert.Equal(TestNow.AddDays(-1), stored.UpdatedAtUtc);
            Assert.Null(stored.ResolvedAtUtc);
            Assert.Single(stored.Messages);
            Assert.Empty(audits);
            Assert.Equal(0, notifications.CreateCalls);
            return;
        }

        Assert.Equal(TestNow, stored.UpdatedAtUtc);

        Assert.All(audits, entry =>
        {
            Assert.Equal(users.AdminId, entry.ActorUserId);
            Assert.Equal(ticket.Id, entry.TargetId);
            Assert.Equal(TestNow, entry.OccurredAtUtc);
        });

        Assert.Single(audits.Select(entry => entry.OperationId).Distinct());

        switch (operationKind)
        {
            case 1:
            {
                Assert.Equal(SupportTicketStatus.InProgress, stored.Status);
                Assert.Equal(2, stored.Messages.Count);
                Assert.Equal(2, audits.Length);

                SupportTicketMessage reply =
                    Assert.Single(stored.Messages, item => item.IsAdmin);

                Assert.Equal(users.AdminId, reply.SenderId);
                Assert.Equal("Admin test reply.", reply.Body);

                AuditLogEntry replyAudit = Assert.Single(audits, item => item.Action == AuditAction.SupportReplyAdded);

                Assert.Equal(reply.Id, replyAudit.RelatedEntityId);
                Assert.Null(replyAudit.PreviousValue);
                Assert.Null(replyAudit.CurrentValue);

                AssertAudit(
                    Assert.Single(audits, item => item.Action == AuditAction.SupportStatusChanged),
                    users.AdminId,
                    ticket.Id,
                    AuditAction.SupportStatusChanged,
                    (int)SupportTicketStatus.Open,
                    (int)SupportTicketStatus.InProgress);

                Assert.Equal(1, notifications.CreateCalls);
                break;
            }

            case 2:
                Assert.Equal(SupportTicketStatus.Resolved, stored.Status);
                Assert.Equal(TestNow, stored.ResolvedAtUtc);
                Assert.Single(stored.Messages);

                AssertAudit(
                    Assert.Single(audits),
                    users.AdminId,
                    ticket.Id,
                    AuditAction.SupportStatusChanged,
                    (int)SupportTicketStatus.Open,
                    (int)SupportTicketStatus.Resolved);

                Assert.Equal(1, notifications.CreateCalls);
                break;

            case 3:
                Assert.Equal(SupportTicketPriority.High, stored.Priority);
                Assert.Equal(SupportTicketStatus.Open, stored.Status);
                Assert.Single(stored.Messages);

                AssertAudit(
                    Assert.Single(audits),
                    users.AdminId,
                    ticket.Id,
                    AuditAction.SupportPriorityChanged,
                    (int)SupportTicketPriority.Normal,
                    (int)SupportTicketPriority.High);

                Assert.Equal(0, notifications.CreateCalls);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(operationKind));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PaidListingModerationPreservesAuditAtomicity(
        bool rejectAudit)
    {
        TestUsers users = await SeedUsersAsync();

        Category category = Category.Create(
            null, $"paid-audit-{Guid.NewGuid():N}", null, 0);

        Listing listing = Listing.Create(
            users.OwnerId,
            category.Id,
            "Paid audit test listing",
            "Listing created for an isolated paid activation test.",
            100m,
            "AZN",
            (RentalPeriodUnit)2);

        listing.MarkAsCreated(TestNow.AddDays(-1), users.OwnerId);

        listing.AddImage(
            $"tests/{listing.Id:N}/listing.jpg",
            "image/jpeg",
            1024);

        listing.SubmitForReview();

        Guid walletId;
        Guid topUpTransactionId;

        await using (RentoXDbContext seed = fixture.CreateContext())
        {
            seed.Categories.Add(category);
            seed.Listings.Add(listing);
            await seed.SaveChangesAsync();

            var seedWallet =
                new RentoX.Infrastructure.Wallets.WalletService(seed, Clock);

            WalletOperationResult topUp =
                await seedWallet.CreditAsync(
                    new CreditWalletCommand(
                        users.OwnerId,
                        10m,
                        (RentoX.Domain.Wallets.Enums.WalletTransactionType)1,
                        "Isolated audit test balance",
                        null,
                        $"audit-topup-{Guid.NewGuid():N}"));

            Assert.Equal(10m, topUp.Wallet.Balance);

            walletId = topUp.Wallet.WalletId;
            topUpTransactionId = topUp.Transaction.Id;
        }

        var actor = new TestActor(users.AdminId);
        var probe = new AuditFailureInterceptor(rejectAudit);
        var notifications = new NotificationSpy(fixture, listing.Id);

        Guid debitTransactionId;

        await using (RentoXDbContext db = CreateServiceContext(actor, probe))
        {
            var wallet = new ObservedWallet(
                new RentoX.Infrastructure.Wallets.WalletService(db, Clock));

            var service = new ListingModerationService(
                dbContext: db,
                clock: Clock,
                pricingService: new PaidPricing(),
                walletService: wallet,
                notificationService: notifications,
                currentUserContext: actor);

            await ExecuteAsync(
                () => service.ApproveAsync(listing.Id),
                rejectAudit,
                probe);

            // DebitAsync completed before the later audit write.
            WalletOperationResult debit =
                Assert.IsType<WalletOperationResult>(wallet.LastDebit);

            Assert.False(debit.WasAlreadyProcessed);
            Assert.Equal(9m, debit.Wallet.Balance);
            Assert.Equal(1m, debit.Transaction.Amount);
            Assert.Equal(10m, debit.Transaction.BalanceBefore);
            Assert.Equal(9m, debit.Transaction.BalanceAfter);
            Assert.Equal(listing.Id, debit.Transaction.RelatedEntityId);

            debitTransactionId = debit.Transaction.Id;
        }

        await using RentoXDbContext verification = fixture.CreateContext();

        Listing stored = await verification.Listings
            .AsNoTracking()
            .SingleAsync(item => item.Id == listing.Id);

        AuditLogEntry[] audits =
            await LoadAuditAsync(verification, listing.Id);

        int cycleCount = await verification.ListingBillingCycles
            .CountAsync(item => item.ListingId == listing.Id);

        var verificationWallet =
            new RentoX.Infrastructure.Wallets.WalletService(
                verification, Clock);

        WalletBalanceResult balance =
            await verificationWallet.GetAsync(users.OwnerId);

        WalletTransactionPageResult transactions =
            await verificationWallet.GetTransactionsAsync(
                users.OwnerId, 1, 20);

        Assert.Equal(walletId, balance.WalletId);

        Assert.Contains(
            transactions.Items,
            item => item.Id == topUpTransactionId);

        if (rejectAudit)
        {
            Assert.Equal(10m, balance.Balance);
            Assert.Equal(1L, transactions.TotalCount);
            Assert.Single(transactions.Items);

            Assert.DoesNotContain(
                transactions.Items,
                item => item.Id == debitTransactionId);

            Assert.Equal(ListingStatus.PendingReview, stored.Status);
            Assert.Null(stored.PublishedAtUtc);
            Assert.Null(stored.ExpiresAtUtc);
            Assert.Equal(0, cycleCount);
            Assert.Empty(audits);
            Assert.Equal(0, notifications.CreateCalls);
        }
        else
        {
            Assert.Equal(9m, balance.Balance);
            Assert.Equal(2L, transactions.TotalCount);

            WalletTransactionResult debit = Assert.Single(
                transactions.Items,
                item => item.Id == debitTransactionId);

            Assert.Equal(2, debit.Direction);
            Assert.Equal(
                (int)RentoX.Domain.Wallets.Enums.WalletTransactionType.ListingFee,
                debit.Type);

            Assert.Equal(1m, debit.Amount);
            Assert.Equal(10m, debit.BalanceBefore);
            Assert.Equal(9m, debit.BalanceAfter);
            Assert.Equal(listing.Id, debit.RelatedEntityId);

            Assert.Equal(
                $"listing-activation:{listing.Id}:cycle:1",
                debit.IdempotencyKey);

            Assert.Equal(ListingStatus.Active, stored.Status);
            Assert.Equal(TestNow, stored.PublishedAtUtc);
            Assert.Equal(TestNow.AddDays(30), stored.ExpiresAtUtc);
            Assert.Equal(1, cycleCount);

            AssertAudit(
                Assert.Single(audits),
                users.AdminId,
                listing.Id,
                AuditAction.ListingStatusChanged,
                (int)ListingStatus.PendingReview,
                (int)ListingStatus.Active);

            Assert.Equal(1, notifications.CreateCalls);
        }
    }

    private RentoXDbContext CreateServiceContext(
        TestActor actor,
        AuditFailureInterceptor probe)
    {
        using RentoXDbContext template = fixture.CreateContext();

        string connectionString =
            template.Database.GetConnectionString()
            ?? throw new InvalidOperationException(
                "Test database connection string is missing.");

        DbContextOptions<RentoXDbContext> options =
            new DbContextOptionsBuilder<RentoXDbContext>()
                .UseNpgsql(connectionString)
                .AddInterceptors(
                    new AuditableEntityInterceptor(Clock, actor),
                    probe)
                .Options;

        return new RentoXDbContext(options);
    }

    private async Task<TestUsers> SeedUsersAsync()
    {
        Guid ownerId = Guid.NewGuid();
        Guid adminId = Guid.NewGuid();

        await using RentoXDbContext db = fixture.CreateContext();

        foreach (Guid id in new[] { ownerId, adminId })
        {
            string userName = $"audit-{id:N}";

            db.Users.Add(new AppUser
            {
                Id = id,
                UserName = userName,
                NormalizedUserName = userName.ToUpperInvariant(),
                SecurityStamp = Guid.NewGuid().ToString("N"),
                RegisteredAtUtc = TestNow.AddDays(-2)
            });
        }

        await db.SaveChangesAsync();

        return new TestUsers(ownerId, adminId);
    }

    private static async Task ExecuteAsync(
        Func<Task> operation,
        bool rejectAudit,
        AuditFailureInterceptor probe)
    {
        if (rejectAudit)
        {
            DbUpdateException error =
                await Assert.ThrowsAsync<DbUpdateException>(operation);

            PostgresException postgres =
                Assert.IsType<PostgresException>(error.InnerException);

            Assert.Equal("23514", postgres.SqlState);
        }
        else
        {
            await operation();
        }

        Assert.True(probe.ObservedEntries > 0);
    }

    private static Task<AuditLogEntry[]> LoadAuditAsync(
        RentoXDbContext db,
        Guid targetId)
    {
        return db.Set<AuditLogEntry>()
            .AsNoTracking()
            .Where(entry => entry.TargetId == targetId)
            .ToArrayAsync();
    }

    private static void AssertAudit(
        AuditLogEntry entry,
        Guid actorId,
        Guid targetId,
        AuditAction action,
        int previousValue,
        int currentValue)
    {
        Assert.NotEqual(Guid.Empty, entry.OperationId);
        Assert.Equal(actorId, entry.ActorUserId);
        Assert.Equal(targetId, entry.TargetId);
        Assert.Equal(action, entry.Action);
        Assert.Equal(previousValue, entry.PreviousValue);
        Assert.Equal(currentValue, entry.CurrentValue);
        Assert.Null(entry.RelatedEntityId);
        Assert.Equal(TestNow, entry.OccurredAtUtc);
    }

    private sealed record TestUsers(Guid OwnerId, Guid AdminId);

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => TestNow;
    }

    private sealed class TestActor(Guid userId) : ICurrentUserContext
    {
        public Guid? UserId => userId;

        public bool IsAuthenticated => true;
    }

    private sealed class AuditFailureInterceptor(bool rejectAudit)
        : SaveChangesInterceptor
    {
        public int ObservedEntries { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is not null)
            {
                foreach (var entry in eventData.Context.ChangeTracker
                             .Entries<AuditLogEntry>()
                             .Where(item => item.State == EntityState.Added))
                {
                    ObservedEntries++;

                    if (rejectAudit)
                    {
                        // Invalid only in this test context:
                        // PostgreSQL must reject the audit INSERT.
                        entry.Property(item => item.Action).CurrentValue =
                            (AuditAction)0;
                    }
                }
            }

            return base.SavingChangesAsync(
                eventData, result, cancellationToken);
        }
    }

    private sealed class PaidPricing : IListingActivationPricingService
    {
        public Task<ListingActivationPricingResult> GetAsync(
            Guid ownerId,
            Guid listingId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ListingActivationPricingResult(
                listingId, ownerId, 5, 5, true, 1m, "AZN"));
        }
    }

    private sealed class ObservedWallet(IWalletService inner) : IWalletService
    {
        public WalletOperationResult? LastDebit { get; private set; }

        public Task<WalletBalanceResult> GetAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return inner.GetAsync(userId, cancellationToken);
        }

        public Task<WalletOperationResult> CreditAsync(
            CreditWalletCommand command,
            CancellationToken cancellationToken = default)
        {
            return inner.CreditAsync(command, cancellationToken);
        }

        public async Task<WalletOperationResult> DebitAsync(
            DebitWalletCommand command,
            CancellationToken cancellationToken = default)
        {
            WalletOperationResult result =
                await inner.DebitAsync(command, cancellationToken);

            LastDebit = result;
            return result;
        }

        public Task<WalletTransactionPageResult> GetTransactionsAsync(
            Guid userId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            return inner.GetTransactionsAsync(
                userId, page, pageSize, cancellationToken);
        }
    }

    private sealed class FreePricing : IListingActivationPricingService
    {
        public Task<ListingActivationPricingResult> GetAsync(
            Guid ownerId,
            Guid listingId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ListingActivationPricingResult(
                listingId, ownerId, 0, 5, false, 0m, "AZN"));
        }
    }

    private sealed class UnusedWallet : IWalletService
    {
        public Task<WalletBalanceResult> GetAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "The free activation test must not access the wallet.");
        }

        public Task<WalletOperationResult> CreditAsync(
            CreditWalletCommand command,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "The free activation test must not credit the wallet.");
        }

        public Task<WalletOperationResult> DebitAsync(
            DebitWalletCommand command,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "The free activation test must not debit the wallet.");
        }

        public Task<WalletTransactionPageResult> GetTransactionsAsync(
            Guid userId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "The free activation test must not query wallet transactions.");
        }
    }

    private sealed class NotificationSpy(
        AuditPostgresFixture database,
        Guid targetId)
        : INotificationService
    {
        public int CreateCalls { get; private set; }

        public async Task<NotificationResult> CreateAsync(
            CreateNotificationCommand command,
            CancellationToken cancellationToken = default)
        {
            CreateCalls++;

            // A separate connection must already see the committed audit
            // before the service attempts to create a notification.
            await using RentoXDbContext verification =
                database.CreateContext();

            Assert.True(
                await verification.Set<AuditLogEntry>()
                    .AsNoTracking()
                    .AnyAsync(
                        entry => entry.TargetId == targetId,
                        cancellationToken));

            return new NotificationResult(
                Guid.NewGuid(),
                command.UserId,
                command.Type,
                command.Title,
                command.Body,
                command.RelatedEntityId,
                command.ActionUrl,
                TestNow,
                null);
        }

        public Task<PagedResult<NotificationResult>> GetAsync(
            Guid userId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<int> GetUnreadCountAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> MarkAsReadAsync(
            Guid userId,
            Guid notificationId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<int> MarkAllAsReadAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}