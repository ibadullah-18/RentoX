using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using RentoX.Api.Authentication;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Infrastructure.Authentication;

namespace RentoX.IntegrationTests.Authentication;

public sealed class JwtIdentityHttpTests
    : IAsyncLifetime, IAsyncDisposable
{
    private static readonly Guid TestUserId =
        Guid.Parse("0f91f73e-642e-4d6e-a962-2a7f55dd30cb");

    private static readonly JwtOptions Settings = new()
    {
        Issuer = "RentoX.JwtTests",
        Audience = "RentoX.JwtTestClients",
        SigningKey = new string('s', 64)
    };

    private WebApplication? _application;
    private HttpClient? _client;

    public async Task InitializeAsync()
    {
        WebApplicationBuilder builder =
            WebApplication.CreateBuilder(
                new WebApplicationOptions
                {
                    EnvironmentName = "Testing"
                });

        builder.Configuration.Sources.Clear();
        builder.Logging.ClearProviders();

        builder.WebHost.UseTestServer();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<
            ICurrentUserContext,
            HttpCurrentUserContext>();

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
                JwtBearerConfiguration.Configure(options, Settings));

        builder.Services.AddAuthorization();

        WebApplication app = builder.Build();
        _application = app;

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet(
                "/api/probe",
                static (ICurrentUserContext user) =>
                    Results.Text(user.UserId?.ToString() ?? string.Empty))
            .RequireAuthorization();

        app.MapGet(
                "/hubs/conversations/probe",
                static () => Results.NoContent())
            .RequireAuthorization();

        app.MapGet(
                "/hubs/notifications/probe",
                static () => Results.NoContent())
            .RequireAuthorization();

        app.MapGet(
                "/admin-probe",
                static () => Results.NoContent())
            .RequireAuthorization(policy =>
                policy.RequireRole("SuperAdmin"));

        await app.StartAsync();
        _client = app.GetTestClient();
    }

    [Theory]
    [InlineData("standard")]
    [InlineData("sub-only")]
    [InlineData("name-only")]
    public async Task ValidIdentityResolvesToTheExpectedUser(
        string variant)
    {
        using HttpResponseMessage response =
            await SendAsync("/api/probe", CreateToken(variant));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Equal(
            TestUserId.ToString(),
            await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("wrong-key")]
    [InlineData("wrong-issuer")]
    [InlineData("wrong-audience")]
    [InlineData("expired")]
    [InlineData("future")]
    [InlineData("unsigned")]
    [InlineData("no-expiration")]
    [InlineData("other-algorithm")]
    [InlineData("invalid-user")]
    [InlineData("empty-user")]
    [InlineData("conflicting-user")]
    [InlineData("missing-user")]
    public async Task InvalidTokenReturnsUnauthorized(string variant)
    {
        using HttpResponseMessage response =
            await SendAsync("/api/probe", CreateToken(variant));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task MissingTokenReturnsUnauthorized()
    {
        using HttpResponseMessage response =
            await SendAsync("/api/probe");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Theory]
    [InlineData("/hubs/conversations/probe", true)]
    [InlineData("/hubs/notifications/probe", true)]
    [InlineData("/api/probe", false)]
    public async Task QueryTokenIsAcceptedOnlyForHubPaths(
        string path,
        bool allowed)
    {
        string token = Uri.EscapeDataString(CreateToken());

        using HttpResponseMessage response =
            await SendAsync(path + "?access_token=" + token);

        Assert.Equal(
            allowed
                ? HttpStatusCode.NoContent
                : HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task QueryTokenDoesNotOverrideAuthorizationHeader()
    {
        string token = Uri.EscapeDataString(CreateToken());

        using HttpResponseMessage response = await SendAsync(
            "/hubs/conversations/probe?access_token=" + token,
            CreateToken("wrong-key"));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Theory]
    [InlineData("User", false)]
    [InlineData("SuperAdmin", true)]
    public async Task RoleAuthorizationRemainsEffective(
        string role,
        bool allowed)
    {
        using HttpResponseMessage response = await SendAsync(
            "/admin-probe",
            CreateToken(role: role));

        Assert.Equal(
            allowed
                ? HttpStatusCode.NoContent
                : HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    private async Task<HttpResponseMessage> SendAsync(
        string path,
        string? token = null)
    {
        HttpClient client = _client
            ?? throw new InvalidOperationException(
                "The test application has not started.");

        using HttpRequestMessage request =
            new(HttpMethod.Get, path);

        if (token is not null)
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        return await client.SendAsync(request);
    }

    private static string CreateToken(
        string variant = "standard",
        string role = "User")
    {
        string userId = variant switch
        {
            "invalid-user" => "not-a-guid",
            "empty-user" => Guid.Empty.ToString(),
            _ => TestUserId.ToString()
        };

        List<Claim> claims = [];

        if (variant != "missing-user")
        {
            if (variant != "name-only")
            {
                claims.Add(new Claim(
                    JwtRegisteredClaimNames.Sub,
                    userId));
            }

            if (variant != "sub-only")
            {
                claims.Add(new Claim(
                    ClaimTypes.NameIdentifier,
                    variant == "conflicting-user"
                        ? "19259c6b-e1ad-496a-939f-95d13f929c96"
                        : userId));
            }
        }

        claims.Add(new Claim(ClaimTypes.Role, role));

        DateTime now = DateTime.UtcNow;

        DateTime notBefore = variant == "future"
            ? now.AddMinutes(2)
            : now.AddMinutes(-10);

        DateTime? expires = variant switch
        {
            "expired" => now.AddMinutes(-2),
            "no-expiration" => null,
            _ => now.AddMinutes(5)
        };

        string signingKey = variant == "wrong-key"
            ? new string('x', 64)
            : Settings.SigningKey;

        string algorithm = variant == "other-algorithm"
            ? SecurityAlgorithms.HmacSha384
            : SecurityAlgorithms.HmacSha256;

        SigningCredentials? credentials = variant == "unsigned"
            ? null
            : new SigningCredentials(
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(signingKey)),
                algorithm);

        JwtSecurityToken jwt = new(
            issuer: variant == "wrong-issuer"
                ? "OtherIssuer"
                : Settings.Issuer,
            audience: variant == "wrong-audience"
                ? "OtherAudience"
                : Settings.Audience,
            claims: claims,
            notBefore: notBefore,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        return DisposeAsync().AsTask();
    }

    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();

        if (_application is not null)
        {
            await _application.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}
