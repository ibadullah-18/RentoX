using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using RentoX.Domain.Auditing;
using RentoX.Domain.Listings.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.IntegrationTests.Auditing;

public sealed class AuditHttpTests(
    AuditHttpFixture fixture)
    : IClassFixture<AuditHttpFixture>
{
    private const string Endpoint = "/api/admin/audit-logs";

    [Fact]
    public async Task AnonymousRequestReturnsUnauthorized()
    {
        using HttpClient client = fixture.CreateClient();

        using HttpResponseMessage response =
            await client.GetAsync(Endpoint);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("User", HttpStatusCode.Forbidden)]
    [InlineData("Admin", HttpStatusCode.Forbidden)]
    [InlineData("SuperAdmin", HttpStatusCode.OK)]
    public async Task AccessDependsOnTheValidatedRole(
        string role,
        HttpStatusCode expectedStatus)
    {
        using HttpClient client = fixture.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                fixture.CreateToken(role));

        using HttpResponseMessage response =
            await client.GetAsync(Endpoint);

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Fact]
    public async Task InvalidSignatureReturnsUnauthorized()
    {
        using HttpClient client = fixture.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                fixture.CreateToken(
                    "SuperAdmin",
                    invalidSignature: true));

        using HttpResponseMessage response =
            await client.GetAsync(Endpoint);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExpiredTokenReturnsUnauthorized()
    {
        using HttpClient client = fixture.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                fixture.CreateToken(
                    "SuperAdmin",
                    expired: true));

        using HttpResponseMessage response =
            await client.GetAsync(Endpoint);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task QueryStringTokenIsNotAcceptedForAuditEndpoint()
    {
        using HttpClient client = fixture.CreateClient();

        string token = fixture.CreateToken("SuperAdmin");

        using HttpResponseMessage response =
            await client.GetAsync(
                Endpoint + "?access_token=" + Uri.EscapeDataString(token));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InvalidFilterReturnsBadRequestForSuperAdmin()
    {
        using HttpClient client = fixture.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                fixture.CreateToken("SuperAdmin"));

        using HttpResponseMessage response =
            await client.GetAsync(Endpoint + "?pageSize=101");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SuperAdminReceivesTheMatchingStoredAuditEntry()
    {
        Guid operationId = Guid.NewGuid();
        Guid actorId = Guid.NewGuid();
        Guid targetId = Guid.NewGuid();

        AuditLogEntry entry =
            AuditLogEntry.ForListingStatusChange(
                operationId,
                actorId,
                targetId,
                ListingStatus.PendingReview,
                ListingStatus.Active,
                DateTimeOffset.UtcNow.AddMinutes(-1));

        await using (RentoXDbContext db = fixture.CreateContext())
        {
            db.Set<AuditLogEntry>().Add(entry);
            await db.SaveChangesAsync();
        }

        using HttpClient client = fixture.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                fixture.CreateToken("SuperAdmin"));

        string uri = string.Create(
            CultureInfo.InvariantCulture,
            $"{Endpoint}?operationId={operationId:D}&actorUserId={actorId:D}&targetId={targetId:D}&action=1");

        using HttpResponseMessage response =
            await client.GetAsync(uri);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore == true);

        await using Stream stream =
            await response.Content.ReadAsStreamAsync();

        using JsonDocument document =
            await JsonDocument.ParseAsync(stream);

        JsonElement root = document.RootElement;
        JsonElement items = root.GetProperty("items");

        Assert.Equal(1, root.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, items.GetArrayLength());

        JsonElement item = items[0];

        Assert.Equal(entry.Id, item.GetProperty("id").GetGuid());
        Assert.Equal(operationId, item.GetProperty("operationId").GetGuid());
        Assert.Equal(actorId, item.GetProperty("actorUserId").GetGuid());
        Assert.Equal(targetId, item.GetProperty("targetId").GetGuid());
        Assert.Equal(1, item.GetProperty("action").GetInt32());
        Assert.Equal(2, item.GetProperty("previousValue").GetInt32());
        Assert.Equal(3, item.GetProperty("currentValue").GetInt32());
    }
}