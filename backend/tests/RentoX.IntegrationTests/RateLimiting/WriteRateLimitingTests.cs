using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using RentoX.Api.RateLimiting;

namespace RentoX.IntegrationTests.RateLimiting;

public sealed class WriteRateLimitingTests
{
    [Fact]
    public async Task ReportLimitReturns429AndRetryAfter()
    {
        await using WebApplication app = await StartApplicationAsync();
        using HttpClient client = app.GetTestClient();

        client.DefaultRequestHeaders.Add(
            "X-Test-User",
            Guid.NewGuid().ToString());

        await ExhaustReportQuotaAsync(client);

        using HttpResponseMessage response =
            await client.PostAsync("/reports", null);

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            response.StatusCode);

        Assert.NotNull(response.Headers.RetryAfter);
        Assert.True(response.Headers.CacheControl?.NoStore == true);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        using JsonDocument body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(
            429,
            body.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task AnotherUserHasIndependentQuota()
    {
        await using WebApplication app = await StartApplicationAsync();
        using HttpClient firstClient = app.GetTestClient();
        using HttpClient secondClient = app.GetTestClient();

        firstClient.DefaultRequestHeaders.Add(
            "X-Test-User",
            Guid.NewGuid().ToString());

        secondClient.DefaultRequestHeaders.Add(
            "X-Test-User",
            Guid.NewGuid().ToString());

        await ExhaustReportQuotaAsync(firstClient);

        using HttpResponseMessage blocked =
            await firstClient.PostAsync("/reports", null);

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            blocked.StatusCode);

        using HttpResponseMessage allowed =
            await secondClient.PostAsync("/reports", null);

        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task ReportQuotaDoesNotConsumeMessageQuota()
    {
        await using WebApplication app = await StartApplicationAsync();
        using HttpClient client = app.GetTestClient();

        client.DefaultRequestHeaders.Add(
            "X-Test-User",
            Guid.NewGuid().ToString());

        await ExhaustReportQuotaAsync(client);

        using HttpResponseMessage blocked =
            await client.PostAsync("/reports", null);

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            blocked.StatusCode);

        using HttpResponseMessage allowed =
            await client.PostAsync("/messages", null);

        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    private static async Task ExhaustReportQuotaAsync(
        HttpClient client)
    {
        for (int index = 0; index < 5; index++)
        {
            using HttpResponseMessage response =
                await client.PostAsync("/reports", null);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    private static async Task<WebApplication> StartApplicationAsync()
    {
        WebApplicationBuilder builder =
            WebApplication.CreateBuilder(
                new WebApplicationOptions
                {
                    EnvironmentName = "Testing"
                });

        builder.WebHost.UseTestServer();
        builder.Services.AddRentoXWriteRateLimiting();

        WebApplication app = builder.Build();

        app.UseRouting();

        // Only this isolated test host creates a test identity.
        // The real API continues to use JWT authentication.
        app.Use(async (context, next) =>
        {
            string userId =
                context.Request.Headers["X-Test-User"].ToString();

            context.User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(
                            ClaimTypes.NameIdentifier,
                            userId)
                    },
                    "Test"));

            await next(context);
        });

        app.UseRateLimiter();

        app.MapPost("/reports", () => Results.Ok())
            .RequireRateLimiting("conversation-report");

        app.MapPost("/messages", () => Results.Ok())
            .RequireRateLimiting("message-send");

        await app.StartAsync();

        return app;
    }
}
