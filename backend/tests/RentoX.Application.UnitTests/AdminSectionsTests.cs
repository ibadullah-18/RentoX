using System.Security.Claims;
using RentoX.Application.Authorization;
using Xunit;

namespace RentoX.Application.UnitTests;

public sealed class AdminSectionsTests
{
    private static ClaimsPrincipal Person(string role, params string[] sections)
    {
        List<Claim> claims = [new(ClaimTypes.Role, role)];
        claims.AddRange(sections.Select(s => new Claim(AdminSections.ClaimType, s)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test", ClaimTypes.Name, ClaimTypes.Role));
    }

    [Fact]
    public void ASuperAdminMayUseEverySection()
    {
        ClaimsPrincipal boss = Person(RoleNames.SuperAdmin);

        Assert.All(AdminSections.All, section => Assert.True(AdminSections.Allows(boss, section)));
    }

    [Fact]
    public void AnAdminWithNoSectionsChosenKeepsAllOfThemExceptTheStaffOne()
    {
        ClaimsPrincipal admin = Person(RoleNames.Admin);

        Assert.All(
            AdminSections.All.Where(section => section != AdminSections.Hr),
            section => Assert.True(AdminSections.Allows(admin, section)));
        // Looking after the staff (their pay and days off) is only ever given on purpose.
        Assert.False(AdminSections.Allows(admin, AdminSections.Hr));
    }

    [Fact]
    public void TheStaffSectionIsAllowedOnlyWhenItIsGiven()
    {
        ClaimsPrincipal watcher = Person(RoleNames.Admin, AdminSections.Hr);
        ClaimsPrincipal other = Person(RoleNames.Admin, AdminSections.Support);

        Assert.True(AdminSections.Allows(watcher, AdminSections.Hr));
        Assert.False(AdminSections.Allows(other, AdminSections.Hr));
    }

    [Fact]
    public void AnAdminWithSectionsChosenIsLimitedToThem()
    {
        ClaimsPrincipal admin = Person(RoleNames.Admin, AdminSections.Support, AdminSections.Reports);

        Assert.True(AdminSections.Allows(admin, AdminSections.Support));
        Assert.True(AdminSections.Allows(admin, AdminSections.Reports));
        Assert.False(AdminSections.Allows(admin, AdminSections.Finance));
        Assert.False(AdminSections.Allows(admin, AdminSections.Users));
    }

    [Fact]
    public void SomeoneWithoutAnAdminRoleMayUseNothingEvenWithTheClaims()
    {
        ClaimsPrincipal user = Person(RoleNames.User, AdminSections.Support);

        Assert.All(AdminSections.All, section => Assert.False(AdminSections.Allows(user, section)));
    }

    [Fact]
    public void ASetIsOneNumberAndAnEmptySetMeansAll()
    {
        int ordinary = (1 << (AdminSections.All.Length - 1)) - 1;

        // Nothing chosen is every ordinary section; the staff section is a bit of its own.
        Assert.Equal(ordinary, AdminSections.ToMask([]));
        Assert.Equal((1 << AdminSections.All.Length) - 1, AdminSections.ToMask(AdminSections.All));
        Assert.NotEqual(ordinary, AdminSections.ToMask(AdminSections.All));
        Assert.Equal(1, AdminSections.ToMask([AdminSections.Listings]));
        Assert.Equal(0b1000, AdminSections.ToMask([AdminSections.Support]));
        Assert.NotEqual(
            AdminSections.ToMask([AdminSections.Support]),
            AdminSections.ToMask([AdminSections.Support, AdminSections.Reports]));
    }
}
