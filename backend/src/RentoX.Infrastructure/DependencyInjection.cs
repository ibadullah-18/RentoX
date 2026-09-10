using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RentoX.Application.Abstractions.Persistence;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Accounts;
using RentoX.Application.Authentication;
using RentoX.Application.Authorization;
using RentoX.Application.Catalog.Categories;
using RentoX.Application.Catalog.Fields;
using RentoX.Application.Favorites;
using RentoX.Application.Files;
using RentoX.Application.Listings;
using RentoX.Application.Listings.Billing;
using RentoX.Application.Listings.Promotions;
using RentoX.Application.Stores;
using RentoX.Application.Users;
using RentoX.Application.Wallets;
using RentoX.Infrastructure.Accounts;
using RentoX.Infrastructure.Authentication;
using RentoX.Infrastructure.Catalog.Categories;
using RentoX.Infrastructure.Catalog.Fields;
using RentoX.Infrastructure.Favorites;
using RentoX.Infrastructure.Files;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Listings;
using RentoX.Infrastructure.Listings.Billing;
using RentoX.Infrastructure.Listings.Promotions;
using RentoX.Infrastructure.Persistence;
using RentoX.Infrastructure.Persistence.Interceptors;
using RentoX.Infrastructure.Stores;
using RentoX.Infrastructure.Time;
using RentoX.Infrastructure.Users;
using RentoX.Infrastructure.Wallets;

namespace RentoX.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
     this IServiceCollection services,
     string connectionString,
     string otpHashingKey,
     JwtOptions jwtOptions,
     IdentitySeedOptions identitySeedOptions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            connectionString);

        services.AddSingleton(identitySeedOptions);

        services.AddScoped<
            IIdentitySeeder,
            IdentitySeeder>();

        services.AddScoped<
            IUserRoleService,
            UserRoleService>();

        services.AddScoped<
            IListingCreationService,
            ListingCreationService>();

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
        services.AddScoped<
            IRefreshTokenService,
            RefreshTokenService>();

        services.AddScoped<
            IAuthSessionService,
            AuthSessionService>();

        services.AddScoped<LoginOtpService>();
        services.AddScoped<CompleteLoginService>();

        services.AddScoped<
            ILoginAccountService,
            LoginAccountService>();

        services.AddScoped<
            IAccountProfileService,
            AccountProfileService>();

        services.AddScoped<
            ICategoryQueryService,
            CategoryQueryService>();

        services.AddScoped<
            ICategoryManagementService,
            CategoryManagementService>();

        services.AddScoped<
            ICategoryFieldManagementService,
            CategoryFieldManagementService>();

        services.AddScoped<
            ICategoryFieldQueryService,
            CategoryFieldQueryService>();

        services.AddScoped<
            ICategoryFieldAdministrationService,
            CategoryFieldAdministrationService>();

        services.AddScoped<
            ICategoryFieldOptionManagementService,
            CategoryFieldOptionManagementService>();

        services.AddSingleton<
            IFileStorage,
            LocalFileStorage>();

        services.AddScoped<
            IListingImageService,
            ListingImageService>();

        services.AddScoped<
            IListingImageManagementService,
            ListingImageManagementService>();

        services.AddScoped<
            IListingQueryService,
            ListingQueryService>();

        services.AddScoped<
            IListingUpdateService,
            ListingUpdateService>();

        services.AddScoped<
            IListingFieldUpdateService,
            ListingFieldUpdateService>();

        services.AddScoped<
            IListingSubmissionService,
            ListingSubmissionService>();

        services.AddScoped<
            IListingModerationService,
            ListingModerationService>();

        services.AddScoped<
            IListingModerationQueryService,
            ListingModerationQueryService>();

        services.AddScoped<
            IPublicListingQueryService,
            PublicListingQueryService>();

        services.AddScoped<
            IListingViewRecorder,
            ListingViewRecorder>();

        services.AddScoped<
            IFavoriteService,
            FavoriteService>();

        services.AddScoped<
            IFavoriteQueryService,
            FavoriteQueryService>();

        services.AddScoped<
            IListingLifecycleService,
            ListingLifecycleService>();

        services.AddScoped<
            IStoreProfileService,
            StoreProfileService>();

        services.AddScoped<
            IStoreImageService,
            StoreImageService>();

        services.AddScoped<
            IStoreSubmissionService,
            StoreSubmissionService>();

        services.AddScoped<
            IStoreModerationService,
            StoreModerationService>();

        services.AddScoped<
            IPublicStoreQueryService,
            PublicStoreQueryService>();

        services.AddScoped<
            IStoreFollowService,
            StoreFollowService>();


        services.AddScoped<
            IWalletService,
            WalletService>();

        services.AddScoped<
            IListingActivationPricingService,
            ListingActivationPricingService>();

        services.AddScoped<
            IListingActivationPaymentService,
            ListingActivationPaymentService>();
        services.AddScoped<
            IListingPromotionPricingService,
            ListingPromotionPricingService>();
        return services;
    }
}


