using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RentoX.Application.Abstractions.Persistence;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Authentication;
using RentoX.Application.Users;
using RentoX.Infrastructure.Authentication;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Persistence;
using RentoX.Infrastructure.Persistence.Interceptors;
using RentoX.Infrastructure.Time;
using RentoX.Infrastructure.Users;

namespace RentoX.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        string otpHashingKey,
        JwtOptions jwtOptions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            connectionString);

        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<RentoXDbContext>(
            (serviceProvider, options) =>
            {
                options.UseNpgsql(connectionString);

                options.AddInterceptors(
                    serviceProvider.GetRequiredService<
                        AuditableEntityInterceptor>());
            });

        services
            .AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = false;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan =
                    TimeSpan.FromMinutes(15);
            })
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<RentoXDbContext>();

        services.AddScoped<IUnitOfWork>(
            provider =>
                provider.GetRequiredService<RentoXDbContext>());

        services.AddScoped<IUserProfileRepository,
            UserProfileRepository>();

        services.AddSingleton<IClock, SystemClock>();

        services.AddSingleton(new OtpOptions
        {
            HashingKey = otpHashingKey
        });

        services.AddSingleton<IOtpCodeGenerator,
            SecureOtpCodeGenerator>();

        services.AddSingleton<IOtpCodeHasher,
            HmacOtpCodeHasher>();

        services.AddSingleton<ISmsSender,
            DevelopmentSmsSender>();

        services.AddScoped<IOtpChallengeRepository,
            OtpChallengeRepository>();

        services.AddScoped<RegistrationOtpService>();

        services.AddScoped<IIdentityUserLookup,
            IdentityUserLookup>();

        services.AddSingleton<IOtpPolicy,
            DefaultOtpPolicy>();

        services.AddScoped<RegistrationOtpService>();

        services.AddScoped<CompleteRegistrationService>();

        services.AddScoped<IRegistrationAccountService,
            RegistrationAccountService>();

        services.AddScoped<IIdentityUserLookup,
            IdentityUserLookup>();

        services.AddSingleton<IOtpPolicy,
            DefaultOtpPolicy>();

        services.AddSingleton(jwtOptions);

        services.AddScoped<ITokenService,
            JwtTokenService>();

        return services;
    }
}