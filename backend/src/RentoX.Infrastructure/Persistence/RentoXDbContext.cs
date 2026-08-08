using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Persistence;
using RentoX.Domain.Authentication;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Catalog.Fields;
using RentoX.Domain.Users;
using RentoX.Infrastructure.Identity;

namespace RentoX.Infrastructure.Persistence;

public sealed class RentoXDbContext(
    DbContextOptions<RentoXDbContext> options)
    : IdentityDbContext<AppUser, AppRole, Guid>(options),
      IUnitOfWork
{
    public DbSet<UserProfile> UserProfiles =>
        Set<UserProfile>();

    public DbSet<OtpChallenge> OtpChallenges =>
        Set<OtpChallenge>();

    public DbSet<RefreshToken> RefreshTokens =>
    Set<RefreshToken>();

    public DbSet<Category> Categories =>
    Set<Category>();

    public DbSet<CategoryTranslation> CategoryTranslations =>
        Set<CategoryTranslation>();

    public DbSet<CategoryField> CategoryFields =>
    Set<CategoryField>();

    public DbSet<CategoryFieldTranslation>
        CategoryFieldTranslations =>
            Set<CategoryFieldTranslation>();

    public DbSet<CategoryFieldOption> CategoryFieldOptions =>
        Set<CategoryFieldOption>();

    public DbSet<CategoryFieldOptionTranslation>
        CategoryFieldOptionTranslations =>
            Set<CategoryFieldOptionTranslation>();

    protected override void OnModelCreating(
        ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(
            typeof(RentoXDbContext).Assembly);

        ConfigureIdentityTables(builder);
    }

    private static void ConfigureIdentityTables(
        ModelBuilder builder)
    {
        builder.Entity<AppUser>()
            .ToTable("users", "identity");

        builder.Entity<AppRole>()
            .ToTable("roles", "identity");

        builder.Entity<IdentityUserRole<Guid>>()
            .ToTable("user_roles", "identity");

        builder.Entity<IdentityUserClaim<Guid>>()
            .ToTable("user_claims", "identity");

        builder.Entity<IdentityUserLogin<Guid>>()
            .ToTable("user_logins", "identity");

        builder.Entity<IdentityRoleClaim<Guid>>()
            .ToTable("role_claims", "identity");

        builder.Entity<IdentityUserToken<Guid>>()
            .ToTable("user_tokens", "identity");
    }
}