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

builder.Services.AddControllers();

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtOptions.SigningKey)),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
    });

builder.Services.AddAuthorization();

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

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet(
    "/health",
    () => Results.Ok(new
    {
        status = "Healthy",
        service = "RentoX.Api",
        timestampUtc = DateTimeOffset.UtcNow
    }));

await app.RunAsync();