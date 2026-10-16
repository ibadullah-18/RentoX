using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using RentoX.Api.Authentication;
using RentoX.Api.ExceptionHandling;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Authorization;
using RentoX.Infrastructure;
using RentoX.Infrastructure.Authentication;
using RentoX.Infrastructure.Identity;

using RentoX.Api.Messaging;
using RentoX.Api.Notifications;
using RentoX.Application.Messaging;
using RentoX.Application.Notifications;

WebApplicationBuilder builder =
    WebApplication.CreateBuilder(args);

string connectionString =
    builder.Configuration.GetConnectionString("Database")
    ?? throw new InvalidOperationException(
        "Database connection string is missing.");

string otpHashingKey =
    builder.Configuration["Otp:HashingKey"]
    ?? throw new InvalidOperationException(
        "OTP hashing key is missing.");

string jwtSigningKey =
    builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException(
        "JWT signing key is missing.");

JwtOptions jwtOptions = new()
{
    Issuer = "RentoX.Api",
    Audience = "RentoX.Clients",
    SigningKey = jwtSigningKey
};

IdentitySeedOptions identitySeedOptions = new()
{
    SuperAdminPhoneNumber =
        builder.Configuration[
            "IdentitySeed:SuperAdminPhoneNumber"]
};

builder.Logging.AddFilter(
    "Microsoft.AspNetCore.Hosting",
    LogLevel.Warning);

// Error tracking: only when an address (DSN) is set in "Sentry:Dsn" (Sentry, or the self-hosted GlitchTip). Without it
// nothing is sent anywhere. Expected "business rule" answers (400) are not errors and are left out; no personal data
// (IP, phone, headers) is attached.
string? sentryDsn = builder.Configuration["Sentry:Dsn"];
if (!string.IsNullOrWhiteSpace(sentryDsn))
{
    builder.WebHost.UseSentry(options =>
    {
        options.Dsn = sentryDsn;
        options.Environment = builder.Environment.EnvironmentName;
        options.SendDefaultPii = false;
        options.TracesSampleRate = 0;
        options.SetBeforeSend(sentryEvent =>
            sentryEvent.Exception is RentoX.Domain.Common.Exceptions.DomainException ? null : sentryEvent);
    });
}

builder.Services.AddSingleton<
    RentoX.Api.Messaging.ConversationRateLimitFilter>();

builder.Services.AddSignalR()
    .AddHubOptions<RentoX.Api.Support.SupportHub>(options =>
    {
        options.MaximumReceiveMessageSize = 4 * 1024;
        options.MaximumParallelInvocationsPerClient = 1;
        options.EnableDetailedErrors = false;

        Microsoft.AspNetCore.SignalR.HubOptionsExtensions
            .AddFilter<
                RentoX.Api.Messaging.ConversationRateLimitFilter>(
                options);
    })
    .AddHubOptions<ConversationsHub>(options =>
    {
        options.MaximumReceiveMessageSize = 8 * 1024;
        options.MaximumParallelInvocationsPerClient = 1;
        options.EnableDetailedErrors = false;

        Microsoft.AspNetCore.SignalR.HubOptionsExtensions
            .AddFilter<
                RentoX.Api.Messaging.ConversationRateLimitFilter>(
                options);
    });

builder.Services.AddRentoXPresence(
    builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException(
        "Redis connection string is missing."));

builder.Services.AddSingleton<
    IConversationEventPublisher,
    SignalRConversationEventPublisher>();

builder.Services.AddSingleton<
    INotificationEventPublisher,
    SignalRNotificationEventPublisher>();

builder.Services.AddSingleton<
    RentoX.Application.Support.ISupportEventPublisher,
    RentoX.Api.Support.SignalRSupportEventPublisher>();

builder.Services.AddScoped<
    Microsoft.AspNetCore.Authorization.IAuthorizationHandler,
    RentoX.Api.Authentication.StaffAccessHandler>();

builder.Services.AddHostedService<RentoX.Api.Hr.AttendanceMonitor>();

RentoX.Api.Hosting.ForwardedHeadersSetup.Configure(builder.Services, builder.Configuration);

builder.Services.AddSingleton<RentoX.Api.Caching.AnonymousResponseCache>();

builder.Services.AddControllers();

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
        RentoX.Api.Authentication.JwtBearerConfiguration.Configure(options, jwtOptions));

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        PolicyNames.AdminAccess,
        policy => policy
            .RequireRole(
                RoleNames.Admin,
                RoleNames.SuperAdmin)
            // The owner can close an Admin's access to the panel.
            .AddRequirements(new RentoX.Api.Authentication.StaffAccessRequirement()));

    // One policy per section of the panel: an Admin sees only the sections chosen for them.
    foreach (string section in AdminSections.All)
    {
        options.AddPolicy(
            AdminSections.Policy(section),
            policy => policy.RequireAssertion(
                context => AdminSections.Allows(context.User, section)));
    }

    options.AddPolicy(
        PolicyNames.SuperAdminOnly,
        policy => policy.RequireRole(
            RoleNames.SuperAdmin));
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.AddPolicy(
        "otp-request",
        httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey:
                    httpContext.Connection
                        .RemoteIpAddress?.ToString()
                    ?? "unknown",
                factory: _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(10),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));

    options.AddPolicy(
        "otp-verification",
        httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey:
                    httpContext.Connection
                        .RemoteIpAddress?.ToString()
                    ?? "unknown",
                factory: _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(10),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));

    options.AddPolicy(
        "token",
        httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey:
                    httpContext.Connection
                        .RemoteIpAddress?.ToString()
                    ?? "unknown",
                factory: _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
});

RentoX.Api.RateLimiting.WriteRateLimitingExtensions
    .AddRentoXWriteRateLimiting(builder.Services);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "bearer",
        new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "RentoX JWT access token"
        });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(
                "bearer",
                document)] = []
        });
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<
    GlobalExceptionHandler>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<
    ICurrentUserContext,
    HttpCurrentUserContext>();

builder.Services.AddInfrastructure(
    connectionString,
    otpHashingKey,
    jwtOptions,
    identitySeedOptions);

WebApplication app = builder.Build();

await using (AsyncServiceScope scope =
    app.Services.CreateAsyncScope())
{
    IIdentitySeeder identitySeeder =
        scope.ServiceProvider
            .GetRequiredService<IIdentitySeeder>();

    await identitySeeder.SeedAsync();
}

// First: the real address of the visitor (from the proxy) is what the rate limits and the logs must see.
app.UseForwardedHeaders();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "RentoX API v1");

        options.RoutePrefix = "swagger";
        options.DocumentTitle = "RentoX API";
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapHub<ConversationsHub>(
    "/hubs/conversations",
    options =>
        options.CloseOnAuthenticationExpiration = true)
    .RequireAuthorization();

app.MapHub<RentoX.Api.Support.SupportHub>(
    "/hubs/support",
    options =>
        options.CloseOnAuthenticationExpiration = true)
    .RequireAuthorization();

app.MapHub<NotificationsHub>(
    "/hubs/notifications",
    options =>
        options.CloseOnAuthenticationExpiration = true)
    .RequireAuthorization();

app.MapControllers();

app.MapGet(
    "/health",
    () => Results.Ok(new
    {
        status = "Healthy",
        service = "RentoX.Api",
        timestampUtc = DateTimeOffset.UtcNow
    }));

// "Ready" means the things the API cannot work without answer: the database and Redis. The plain /health only says the
// process is running (for a restart check); this one is for monitoring and for the deploy to wait on.
app.MapGet(
    "/health/ready",
    async (
        RentoX.Infrastructure.Persistence.RentoXDbContext database,
        StackExchange.Redis.IConnectionMultiplexer redis,
        CancellationToken cancellationToken) =>
    {
        bool databaseUp;
        try
        {
            databaseUp = await database.Database.CanConnectAsync(cancellationToken);
        }
        catch (Exception)
        {
            databaseUp = false;
        }

        bool redisUp;
        try
        {
            await redis.GetDatabase().PingAsync();
            redisUp = true;
        }
        catch (Exception)
        {
            redisUp = false;
        }

        object body = new
        {
            status = databaseUp && redisUp ? "Ready" : "NotReady",
            database = databaseUp ? "up" : "down",
            redis = redisUp ? "up" : "down"
        };

        return databaseUp && redisUp
            ? Results.Ok(body)
            : Results.Json(body, statusCode: StatusCodes.Status503ServiceUnavailable);
    });

await app.RunAsync();
