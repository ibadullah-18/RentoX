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
using RentoX.Application.Notifications;
using RentoX.Application.Stores;
using RentoX.Application.Support;
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
using RentoX.Infrastructure.Listings.Search;
using RentoX.Infrastructure.Listings.Billing;
using RentoX.Infrastructure.Listings.Promotions;
using RentoX.Infrastructure.Notifications;
using RentoX.Infrastructure.Persistence;
using RentoX.Infrastructure.Persistence.Interceptors;
using RentoX.Infrastructure.Stores;
using RentoX.Infrastructure.Support;
using RentoX.Infrastructure.Time;
using RentoX.Infrastructure.Users;
using RentoX.Infrastructure.Wallets;

using RentoX.Application.Messaging;
using RentoX.Infrastructure.Messaging;
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
            RentoX.Application.Accounts.IProfileImageService,
            RentoX.Infrastructure.Accounts.ProfileImageService>();

        services.AddScoped<
            RentoX.Application.Admin.IAdminCatalogReadService,
            RentoX.Infrastructure.Admin.AdminCatalogReadService>();

        services.AddScoped<
            RentoX.Application.Admin.IAdminStoreSearchService,
            RentoX.Infrastructure.Admin.AdminStoreSearchService>();

        services.AddScoped<
            RentoX.Application.Admin.IStaffManagementService,
            RentoX.Infrastructure.Admin.StaffManagementService>();

        services.AddScoped<
            RentoX.Application.Gifts.IGiftService,
            RentoX.Infrastructure.Gifts.GiftService>();

        services.AddScoped<
            RentoX.Application.Finance.IFinanceService,
            RentoX.Infrastructure.Finance.FinanceService>();

        services.AddScoped<
            RentoX.Application.Stores.IStoreVipService,
            RentoX.Infrastructure.Stores.StoreVipService>();

        services.AddScoped<
            RentoX.Application.Stores.IStoreStatsService,
            RentoX.Infrastructure.Stores.StoreStatsService>();

        services.AddScoped<
            RentoX.Application.Hr.IStaffHrService,
            RentoX.Infrastructure.Hr.StaffHrService>();

        services.AddMemoryCache();

        services.AddScoped<
            RentoX.Application.Admin.IUserStatusGuard,
            RentoX.Infrastructure.Admin.UserStatusGuard>();

        services.AddScoped<
            RentoX.Application.Accounts.IAccountNotificationSettingsService,
            RentoX.Infrastructure.Accounts.AccountNotificationSettingsService>();

        services.AddScoped<
            RentoX.Application.Locations.IAdminLocationService,
            RentoX.Infrastructure.Locations.AdminLocationService>();

        services.AddScoped<
            RentoX.Application.Settings.ISystemSettingsService,
            RentoX.Infrastructure.Settings.SystemSettingsService>();

        services.AddScoped<
            RentoX.Application.Admin.IWalletAdjustmentService,
            RentoX.Infrastructure.Admin.WalletAdjustmentService>();

        services.AddScoped<
            RentoX.Application.Admin.IBannedWordService,
            RentoX.Infrastructure.Admin.BannedWordService>();

        services.AddScoped<
            RentoX.Application.Content.IHomeBannerService,
            RentoX.Infrastructure.Content.HomeBannerService>();

        services.AddScoped<
            RentoX.Application.Content.IContentPageService,
            RentoX.Infrastructure.Content.ContentPageService>();

        services.AddScoped<
            RentoX.Application.Content.ISiteInfoService,
            RentoX.Infrastructure.Content.SiteInfoService>();

        services.AddScoped<
            RentoX.Application.Admin.IAnnouncementService,
            RentoX.Infrastructure.Admin.AnnouncementService>();

        services.AddScoped<
            RentoX.Application.Admin.IUserModerationService,
            RentoX.Infrastructure.Admin.UserModerationService>();

        services.AddScoped<
            RentoX.Application.Admin.IAdminUserService,
            RentoX.Infrastructure.Admin.AdminUserService>();

        services.AddScoped<
            RentoX.Application.Admin.IAdminListingSearchService,
            RentoX.Infrastructure.Admin.AdminListingSearchService>();

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

        // The disk or Cloudflare R2, by the setting Storage:Provider.
        services.AddRentoXFileStorage();

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
            IListingSuggestionService,
            ListingSuggestionService>();

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
            INotificationService,
            NotificationService>();

        services.AddScoped<
            IPushDeviceService,
            PushDeviceService>();


        services.AddScoped<
            ISupportTicketService,
            SupportTicketService>();
        services.AddScoped<
            IWalletService,
            WalletService>();

        services.AddScoped<
            RentoX.Application.Wallets.IDemoWalletTopUpService,
            RentoX.Infrastructure.Wallets.DemoWalletTopUpService>();

        services.AddScoped<
            IListingActivationPricingService,
            ListingActivationPricingService>();

        services.AddScoped<
            IListingActivationPaymentService,
            ListingActivationPaymentService>();
        services.AddScoped<
            RentoX.Application.Listings.Billing.ITariffService,
            RentoX.Infrastructure.Listings.Billing.TariffService>();

        services.AddScoped<
            IListingPromotionPricingService,
            ListingPromotionPricingService>();
        services.AddScoped<
            IListingPromotionService,
            ListingPromotionService>();
        services.AddScoped<
            IListingRenewalService,
            ListingRenewalService>();

        services.AddScoped<
            IConversationService,
            ConversationService>();
        services.AddScoped<RentoX.Application.Messaging.IConversationBlockService, RentoX.Infrastructure.Messaging.ConversationBlockService>();
        services.AddScoped<
            RentoX.Application.Locations.ILocationQueryService,
            RentoX.Infrastructure.Locations.LocationQueryService>();

        services.AddScoped<
            RentoX.Application.Accounts.IAccountDeletionService,
            RentoX.Infrastructure.Accounts.AccountDeletionService>();

        services.AddScoped<
            RentoX.Application.Moderation.IContentReportService,
            RentoX.Infrastructure.Moderation.ContentReportService>();

        services.AddScoped<RentoX.Application.Messaging.IConversationReportService, RentoX.Infrastructure.Messaging.ConversationReportService>();
        services.AddScoped<RentoX.Application.Messaging.IMessageImageService, RentoX.Infrastructure.Messaging.MessageImageService>();

        services.AddScoped<RentoX.Application.Analytics.IAdminAnalyticsService, RentoX.Infrastructure.Analytics.AdminAnalyticsService>();

        services.AddScoped<
            RentoX.Application.Auditing.IAuditLogQueryService,
            RentoX.Infrastructure.Auditing.AuditLogQueryService>();

        services.AddScoped<
            IOtpOperationScopeFactory,
            OtpOperationScopeFactory>();

        return services;
    }
}
