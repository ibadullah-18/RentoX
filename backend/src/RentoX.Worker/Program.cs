using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Files;
using RentoX.Application.Listings;
using RentoX.Infrastructure.Files;
using RentoX.Infrastructure.Listings;
using RentoX.Infrastructure.Persistence;
using RentoX.Infrastructure.Time;
using RentoX.Worker;
using RentoX.Worker.PushNotifications;

HostApplicationBuilder builder =
    Host.CreateApplicationBuilder(args);

string connectionString =
    builder.Configuration
        .GetConnectionString("Database")
    ?? throw new InvalidOperationException(
        "Database connection string is missing.");

builder.Services.Configure<ListingMaintenanceOptions>(
    builder.Configuration.GetSection(
        "ListingMaintenance"));

builder.Services.Configure<PushNotificationOptions>(
    builder.Configuration.GetSection(
        PushNotificationOptions.SectionName));

builder.Services.AddDbContext<RentoXDbContext>(
    options =>
        options.UseNpgsql(connectionString));

builder.Services.AddSingleton<IClock, SystemClock>();

builder.Services.AddSingleton<
    IFileStorage,
    LocalFileStorage>();

builder.Services.AddScoped<
    IListingMaintenanceService,
    ListingMaintenanceService>();

builder.Services.AddSingleton<
    IPushNotificationSender,
    FirebasePushNotificationSender>();

builder.Services.AddScoped<
    PushNotificationProcessor>();

builder.Services.AddHostedService<Worker>();

builder.Services.AddHostedService<
    PushNotificationWorker>();

IHost host = builder.Build();

await host.RunAsync();