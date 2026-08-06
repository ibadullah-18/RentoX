using RentoX.Api.Authentication;
using RentoX.Api.ExceptionHandling;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Infrastructure;
using RentoX.Infrastructure.Authentication;

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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<
    ICurrentUserContext,
    HttpCurrentUserContext>();

builder.Services.AddInfrastructure(
    connectionString,
    otpHashingKey,
    jwtOptions);

WebApplication app = builder.Build();

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

app.MapControllers();

app.MapGet("/health", () =>
    Results.Ok(new
    {
        status = "Healthy",
        service = "RentoX.Api",
        timestampUtc = DateTimeOffset.UtcNow
    }));

await app.RunAsync();