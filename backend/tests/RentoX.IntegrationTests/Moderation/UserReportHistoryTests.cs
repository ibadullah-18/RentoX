using Microsoft.EntityFrameworkCore;
using RentoX.Application.Admin;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Messaging;
using RentoX.Domain.Moderation;
using RentoX.Domain.Stores;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Admin;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Persistence;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Moderation;

/// <summary>On the person's page: what others reported about them and how those reports ended.</summary>
public sealed class UserReportHistoryTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ReportsOnTheirListingsStoreAndChatsAreCountedByHowTheyEnded()
    {
        Guid person = await AddUserAsync();
        Guid reporter = await AddUserAsync();
        Guid reviewer = await AddUserAsync();
        Guid listing = await AddListingAsync(person);
        Guid store = await AddStoreAsync(person);
        Guid conversation = await AddConversationAsync(listing, reporter, person);

        await using (RentoXDbContext db = database.CreateContext())
        {
            // Listing report: upheld. Store report: dismissed. Chat report: still waiting.
            ContentReport upheld = ContentReport.Create(
                ContentReportTarget.Listing, listing, reporter, ContentReportReason.Fraud, null, Now.AddDays(-3));
            upheld.Review(ContentReportStatus.Resolved, reviewer, "Təsdiqləndi", Now.AddDays(-2));
            ContentReport dismissed = ContentReport.Create(
                ContentReportTarget.Store, store, reporter, ContentReportReason.Spam, null, Now.AddDays(-2));
            dismissed.Review(ContentReportStatus.Rejected, reviewer, "Əsassız", Now.AddDays(-1));
            ConversationReport waiting = ConversationReport.Create(
                conversation, reporter, ConversationReportReason.Harassment, null, null, Now.AddHours(-1));
            db.Set<ContentReport>().AddRange(upheld, dismissed);
            db.Set<ConversationReport>().Add(waiting);

            // The person's own report about someone else's chat must not count against them.
            Guid otherListing = await AddListingAsync(reporter);
            Guid theirChat = await AddConversationAsync(otherListing, person, reporter);
            db.Set<ConversationReport>().Add(ConversationReport.Create(
                theirChat, person, ConversationReportReason.Spam, null, null, Now.AddHours(-2)));
            await db.SaveChangesAsync();
        }

        await using RentoXDbContext read = database.CreateContext();
        AdminUserReportSummary? reports = (await new AdminUserService(read).GetAsync(person))?.Reports;

        Assert.NotNull(reports);
        Assert.Equal(3, reports.Total);
        Assert.Equal(1, reports.Open);
        Assert.Equal(1, reports.Upheld);
        Assert.Equal(1, reports.Dismissed);
        Assert.Equal("conversation", reports.Recent[0].Kind); // the newest first
        Assert.Contains(reports.Recent, r => r.Kind == "listing" && r.Note == "Təsdiqləndi");
    }

    [Fact]
    public async Task ASpotlessPersonHasNoReports()
    {
        Guid person = await AddUserAsync();

        await using RentoXDbContext read = database.CreateContext();
        AdminUserReportSummary? reports = (await new AdminUserService(read).GetAsync(person))?.Reports;

        Assert.NotNull(reports);
        Assert.Equal(0, reports.Total);
        Assert.Empty(reports.Recent);
    }

    private async Task<Guid> AddUserAsync()
    {
        await using RentoXDbContext db = database.CreateContext();
        Guid id = Guid.NewGuid();
        string userName = $"rep-{id:N}";
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

    private async Task<Guid> AddListingAsync(Guid ownerId)
    {
        await using RentoXDbContext db = database.CreateContext();
        Category category = Category.Create(null, $"rep-{Guid.NewGuid():N}", null, 0);
        category.AddTranslation(CategoryTranslation.Create(category.Id, PreferredLanguage.Azerbaijani, "Test"));
        db.Categories.Add(category);
        Listing listing = Listing.Create(
            ownerId, category.Id, "Şikayət testi", "Təsvir mətni burada yazılıb.", 50m, "AZN", RentalPeriodUnit.Day);
        listing.MarkAsCreated(Now, ownerId);
        listing.AddImage($"tests/rep-{Guid.NewGuid():N}.jpg", "image/jpeg", 100);
        listing.SubmitForReview();
        listing.Publish(Now, TimeSpan.FromDays(30));
        db.Listings.Add(listing);
        await db.SaveChangesAsync();
        return listing.Id;
    }

    private async Task<Guid> AddStoreAsync(Guid ownerId)
    {
        await using RentoXDbContext db = database.CreateContext();
        StoreProfile store = StoreProfile.Create(
            ownerId, "Report test store", "report-test-" + Guid.NewGuid().ToString("N"),
            "A store created for the report history tests.", "+994500000000", null, null, null, null, null, null);
        store.SetLogo("stores/logo.webp");
        store.SubmitForReview();
        store.Approve();
        db.StoreProfiles.Add(store);
        await db.SaveChangesAsync();
        return store.Id;
    }

    private async Task<Guid> AddConversationAsync(Guid listingId, Guid buyerId, Guid sellerId)
    {
        await using RentoXDbContext db = database.CreateContext();
        Conversation conversation = Conversation.Create(listingId, buyerId, sellerId, Now.AddDays(-5));
        db.Set<Conversation>().Add(conversation);
        await db.SaveChangesAsync();
        return conversation.Id;
    }
}
